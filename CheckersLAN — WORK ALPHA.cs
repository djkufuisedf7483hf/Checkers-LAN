using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

class CheckersForm : Form
{
    const int BOARD_SIZE = 8;
    const int CELL_SIZE = 90; 
    const int OFFSET = 40;    

    int[,] board = new int[BOARD_SIZE, BOARD_SIZE];

    int selectedX = -1;
    int selectedY = -1;

    bool isComboMode = false;
    int comboX = -1;
    int comboY = -1;

    bool isServer = false;
    bool isConnected = false;
    bool myTurn = false; 
    int myColor = 1;     // 1 - WHITE, 2 - RED
    bool gameOver = false;

    TcpListener server;
    TcpClient client;
    NetworkStream stream;
    Thread receiveThread;

    TextBox ipTextBox;
    Button btnHost;
    Button btnConnect;

    string statusMessage = "Создайте игру или подключитесь по IP к другу.";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new CheckersForm());
    }

    public CheckersForm()
    {
        this.Text = "шашки по сети!";
        this.ClientSize = new Size(BOARD_SIZE * CELL_SIZE + OFFSET * 2, BOARD_SIZE * CELL_SIZE + OFFSET * 2 + 100);
        this.DoubleBuffered = true; 
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        InitializeBoard();
        BuildNetworkMenu();

        this.MouseClick += new MouseEventHandler(OnCanvasClick);
        this.FormClosing += new FormClosingEventHandler(OnFormClosing);
    }

    void InitializeBoard()
    {
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                board[y, x] = 0;
                if (y < 3 && (x + y) % 2 != 0) board[y, x] = 2; // Красные
                if (y > 4 && (x + y) % 2 != 0) board[y, x] = 1; // Белые
            }
        }
    }

    void BuildNetworkMenu()
    {
        string localIP = "127.0.0.1";
        try
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in localIPs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) { localIP = ip.ToString(); break; }
            }
        }
        catch { }

        btnHost = new Button();
        btnHost.Text = "СОЗДАТЬ ИГРУ (LAN)";
        btnHost.Location = new Point(OFFSET, 10);
        btnHost.Size = new Size(160, 25);
        btnHost.Click += new EventHandler(StartServer);
        this.Controls.Add(btnHost);

        Label ipLabel = new Label();
        ipLabel.Text = "АДРЕС СЕРВЕРА:";
        ipLabel.Location = new Point(OFFSET + 180, 15);
        ipLabel.Size = new Size(90, 20);
        this.Controls.Add(ipLabel);

        ipTextBox = new TextBox();
        ipTextBox.Text = localIP;
        ipTextBox.Location = new Point(OFFSET + 280, 10);
        ipTextBox.Size = new Size(110, 25);
        this.Controls.Add(ipTextBox);

        btnConnect = new Button();
        btnConnect.Text = "ПОДКЛЮЧИТЬСЯ";
        btnConnect.Location = new Point(OFFSET + 400, 10);
        btnConnect.Size = new Size(120, 25);
        btnConnect.Click += new EventHandler(ConnectToServer);
        this.Controls.Add(btnConnect);
    }

    void StartServer(object sender, EventArgs e)
    {
        isServer = true; myColor = 1; myTurn = true; DisableMenu();
        statusMessage = "Ожидание подключения..."; this.Invalidate();

        Thread listenThread = new Thread(new ThreadStart(delegate {
            try
            {
                server = new TcpListener(IPAddress.Any, 12345);
                server.Start();
                client = server.AcceptTcpClient();
                stream = client.GetStream();
                isConnected = true;
                statusMessage = "Игрок подключился! Твой ход.";
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.Start();
            }
            catch { statusMessage = "Ошибка запуска LAN-сервера."; }
            this.Invoke(new MethodInvoker(delegate { this.Invalidate(); }));
        }));
        listenThread.Start();
    }

    void ConnectToServer(object sender, EventArgs e)
    {
        isServer = false; myColor = 2; myTurn = false; DisableMenu();
        statusMessage = "Подключение к " + ipTextBox.Text + "..."; this.Invalidate();

        Thread connectThread = new Thread(new ThreadStart(delegate {
            try
            {
                client = new TcpClient(ipTextBox.Text, 12345);
                stream = client.GetStream();
                isConnected = true;
                statusMessage = "Подключено! Ожидание хода...";
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.Start();
            }
            catch
            {
                statusMessage = "Не удалось подключиться.";
                this.Invoke(new MethodInvoker(delegate { EnableMenu(); }));
            }
            this.Invoke(new MethodInvoker(delegate { this.Invalidate(); }));
        }));
        connectThread.Start();
    }

    void DisableMenu() { btnHost.Enabled = false; btnConnect.Enabled = false; ipTextBox.Enabled = false; }
    void EnableMenu() { btnHost.Enabled = true; btnConnect.Enabled = true; ipTextBox.Enabled = true; }

    void ReceiveData()
    {
        BinaryReader reader = new BinaryReader(stream);
        while (isConnected)
        {
            try
            {
                int x1 = reader.ReadInt32();
                int y1 = reader.ReadInt32();
                int x2 = reader.ReadInt32();
                int y2 = reader.ReadInt32();
                int nextTurnFlag = reader.ReadInt32();

                this.Invoke(new MethodInvoker(delegate {
                    int piece = board[y1, x1];
                    board[y2, x2] = piece;
                    board[y1, x1] = 0;

                    int diffX = x2 - x1;
                    int diffY = y2 - y1;
                    if (Math.Abs(diffX) > 1)
                    {
                        int stepX = diffX > 0 ? 1 : -1;
                        int stepY = diffY > 0 ? 1 : -1;
                        int steps = Math.Abs(diffX);
                        for (int i = 1; i < steps; i++)
                        {
                            int tx = x1 + i * stepX;
                            int ty = y1 + i * stepY;
                            if (board[ty, tx] != 0) { board[ty, tx] = 0; break; }
                        }
                        PlaySound(300);
                    }

                    if (board[y2, x2] == 1 && y2 == 0) board[y2, x2] = 3;
                    if (board[y2, x2] == 2 && y2 == 7) board[y2, x2] = 4;

                    if (nextTurnFlag == 1)
                    {
                        myTurn = true;
                        statusMessage = "Соперник завершил ход. Твой ход!";
                    }
                    else
                    {
                        myTurn = false;
                        statusMessage = "💥 соперник сделал комбо!";
                    }
                    CheckNetworkGameOver();
                    this.Invalidate();
                }));
            }
            catch
            {
                isConnected = false;
                statusMessage = "Связь потеряна.";
                this.Invoke(new MethodInvoker(delegate { EnableMenu(); this.Invalidate(); }));
                break;
            }
        }
    }

    void OnCanvasClick(object sender, MouseEventArgs e)
    {
        if (!isConnected || !myTurn || gameOver) return;

        int clickX = (e.X - OFFSET) / CELL_SIZE;
        int clickY = (e.Y - (OFFSET + 40)) / CELL_SIZE;

        if (clickX < 0 || clickX >= BOARD_SIZE || clickY < 0 || clickY >= BOARD_SIZE) return;

        int piece = board[clickY, clickX];

        if (isComboMode)
        {
            if (clickX == comboX && clickY == comboY) return; 
        }
        else
        {
            if ((myColor == 1 && (piece == 1 || piece == 3)) || (myColor == 2 && (piece == 2 || piece == 4)))
            {
                selectedX = clickX; selectedY = clickY;
                statusMessage = "Шашка выбрана.";
                this.Invalidate(); return;
            }
        }

            if (selectedX != -1 && selectedY != -1 && piece == 0)
        {
            bool validMove = false;
            bool isKill = false;
            int currentPiece = board[selectedY, selectedX];
            int diffX = clickX - selectedX;
            int diffY = clickY - selectedY;

            if ((currentPiece == 3 || currentPiece == 4) && Math.Abs(diffX) == Math.Abs(diffY))
            {
                int stepX = diffX > 0 ? 1 : -1;
                int stepY = diffY > 0 ? 1 : -1;
                int steps = Math.Abs(diffX);
                int piecesOnLine = 0;
                int enemyX = -1, enemyY = -1;

                for (int i = 1; i < steps; i++)
                {
                    int tx = selectedX + i * stepX;
                    int ty = selectedY + i * stepY;
                    if (board[ty, tx] != 0) { piecesOnLine++; enemyX = tx; enemyY = ty; }
                }

                if (piecesOnLine == 0 && !isComboMode)
                {
                    board[clickY, clickX] = currentPiece; board[selectedY, selectedX] = 0; validMove = true;
                }
                else if (piecesOnLine == 1)
                {
                    int target = board[enemyY, enemyX];
                    if ((myColor == 1 && (target == 2 || target == 4)) || (myColor == 2 && (target == 1 || target == 3)))
                    {
                        board[clickY, clickX] = currentPiece; board[selectedY, selectedX] = 0; board[enemyY, enemyX] = 0;
                        validMove = true; isKill = true; PlaySound(600);
                    }
                }
            }
            else if (Math.Abs(diffX) == Math.Abs(diffY))
            {
                if (Math.Abs(diffX) == 1 && !isComboMode)
                {
                    if ((myColor == 1 && diffY == -1) || (myColor == 2 && diffY == 1))
                    {
                        board[clickY, clickX] = currentPiece; board[selectedY, selectedX] = 0; validMove = true;
                    }
                }
                else if (Math.Abs(diffX) == 2)
                {
                    int midX = (selectedX + clickX) / 2;
                    int midY = (selectedY + clickY) / 2;
                    int target = board[midY, midX];
                    if ((myColor == 1 && (target == 2 || target == 4)) || (myColor == 2 && (target == 1 || target == 3)))
                    {
                        board[clickY, clickX] = currentPiece; board[selectedY, selectedX] = 0; board[midY, midX] = 0;
                        validMove = true; isKill = true; PlaySound(600);
                    }
                }
            }

            if (validMove)
            {
                if (board[clickY, clickX] == 1 && clickY == 0) board[clickY, clickX] = 3;
                if (board[clickY, clickX] == 2 && clickY == 7) board[clickY, clickX] = 4;

                int oldX = selectedX;
                int oldY = selectedY;

                bool canKillMore = false;
                if (isKill) { canKillMore = CanPieceKillMore(clickX, clickY); }

                int nextTurnFlag = 1; 
                if (canKillMore)
                {
                    isComboMode = true;
                    comboX = clickX; comboY = clickY;
                    selectedX = clickX; selectedY = clickY; 
                    nextTurnFlag = 0; 
                    statusMessage = "🔥 ДВОЙНОЙ УДАР! Руби дальше этой же шашкой!";
                }
                else
                {
                    isComboMode = false; comboX = -1; comboY = -1; selectedX = -1; selectedY = -1;
                    myTurn = false; 
                    statusMessage = "Ход отправлен сопернику. Ожидание...";
                }

                try
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(oldX); 
                    writer.Write(oldY);
                    writer.Write(clickX); 
                    writer.Write(clickY);
                    writer.Write(nextTurnFlag); 
                    writer.Flush();
                }
                catch { }

                CheckNetworkGameOver();
                this.Invalidate();
            }
        }
    }

    bool CanPieceKillMore(int cx, int cy)
    {
        int p = board[cy, cx];
        int[] dx = { -2, 2, -2, 2 };
        int[] dy = { -2, -2, 2, 2 };

        if (p == 1 || p == 2)
        {
            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i]; int ny = cy + dy[i];
                if (nx >= 0 && nx < BOARD_SIZE && ny >= 0 && ny < BOARD_SIZE)
                {
                    if (board[ny, nx] == 0)
                    {
                        int mx = (cx + nx) / 2; int my = (cy + ny) / 2;
                        int target = board[my, mx];
                        if (p == 1 && (target == 2 || target == 4)) return true;
                        if (p == 2 && (target == 1 || target == 3)) return true;
                    }
                }
            }
        }
        else if (p == 3 || p == 4)
        {
            int[] sX = { -1, 1, -1, 1 }; int[] sY = { -1, -1, 1, 1 };
            for (int d = 0; d < 4; d++)
            {
                int tx = cx; int ty = cy;
                bool foundEnemy = false;
                while (true)
                {
                    tx += sX[d]; ty += sY[d];
                    if (tx < 0 || tx >= BOARD_SIZE || ty < 0 || ty >= BOARD_SIZE) break;
                    int cell = board[ty, tx];
                    if (cell == p) break; 
                    if (!foundEnemy && cell != 0)
                    {
                        if (p == 3 && (cell == 2 || cell == 4)) foundEnemy = true;
                        else if (p == 4 && (cell == 1 || cell == 3)) foundEnemy = true;
                        else break; 
                    }
                    else if (foundEnemy)
                    {
                        if (cell == 0) return true; 
                        else break; 
                    }
                }
            }
        }
        return false;
    }

    void CheckNetworkGameOver()
    {
        int p1 = CountPieces(1) + CountPieces(3);
        int p2 = CountPieces(2) + CountPieces(4);
        if (p1 == 0 || p2 == 0)
        {
            gameOver = true;
            if ((myColor == 1 && p1 > 0) || (myColor == 2 && p2 > 0)) statusMessage = "🏆 ТЫ ВЫИГРАЛ МАТЧ!";
            else statusMessage = "☠️ СОПЕРНИК ОКАЗАЛСЯ СИЛЬНЕЕ.";
            MessageBox.Show(statusMessage, "Конец LAN-игры");
        }
    }

    int CountPieces(int type)
    {
        int count = 0;
        for (int y = 0; y < BOARD_SIZE; y++)
            for (int x = 0; x < BOARD_SIZE; x++)
                if (board[y, x] == type) count++;
        return count;
    }

    void PlaySound(int freq) { Thread t = new Thread(new ThreadStart(delegate { Console.Beep(freq, 15); })); t.Start(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int boardOffsetY = OFFSET + 40;

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                Brush cellBrush = ((x + y) % 2 == 0) ? Brushes.BlanchedAlmond : Brushes.SaddleBrown;
                g.FillRectangle(cellBrush, OFFSET + x * CELL_SIZE, boardOffsetY + y * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                if (x == selectedX && y == selectedY)
                {
                    using (Pen selectPen = new Pen(Color.LimeGreen, 4))
                        g.DrawRectangle(selectPen, OFFSET + x * CELL_SIZE + 2, boardOffsetY + y * CELL_SIZE + 2, CELL_SIZE - 4, CELL_SIZE - 4);
                }
            }
        }
        g.DrawRectangle(Pens.Black, OFFSET, boardOffsetY, BOARD_SIZE * CELL_SIZE, BOARD_SIZE * CELL_SIZE);

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                int cell = board[y, x]; if (cell == 0) continue;
                int radius = CELL_SIZE - 20;
                int pX = OFFSET + x * CELL_SIZE + 10; int pY = boardOffsetY + y * CELL_SIZE + 10;
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(shadowBrush, pX + 3, pY + 4, radius, radius);

                if (cell == 1 || cell == 3)
                {
                    g.FillEllipse(Brushes.White, pX, pY, radius, radius); g.DrawEllipse(Pens.Gray, pX, pY, radius, radius);
                    if (cell == 3) { Font f = new Font("Arial", 22, FontStyle.Bold); g.DrawString("👑", f, Brushes.Gold, pX + 11, pY + 11); }
                }
                else if (cell == 2 || cell == 4)
                {
                    g.FillEllipse(Brushes.Crimson, pX, pY, radius, radius); g.DrawEllipse(Pens.DarkRed, pX, pY, radius, radius);
                    if (cell == 4) { Font f = new Font("Arial", 22, FontStyle.Bold); g.DrawString("👑", f, Brushes.Gold, pX + 11, pY + 11); }
                }
            }
        }

        Font infoFont = new Font("Arial", 11, FontStyle.Bold);
        Brush textBrush = myTurn ? Brushes.DarkGreen : Brushes.DarkBlue;
        string roleInfo = isServer ? " [СЕРВЕР (БЕЛЫЕ)]" : " [КЛИЕНТ (КРАСНЫЕ)]";
        g.FillRectangle(Brushes.LightGray, 0, BOARD_SIZE * CELL_SIZE + boardOffsetY + OFFSET, this.Width, 40);
        g.DrawString(statusMessage + roleInfo, infoFont, textBrush, OFFSET - 20, BOARD_SIZE * CELL_SIZE + boardOffsetY + OFFSET + 10);
    }

    void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        try {
            isConnected = false;
            if (stream != null) stream.Close(); if (client != null) client.Close();
            if (server != null) server.Stop(); if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
        } catch { }
    }
}
