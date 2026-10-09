# Checkers LAN

Простые сетевые шашки для двух игроков, написанные на C# (Windows Forms). Игра создавалась для того, чтобы в неё можно было быстро и удобно играть с другом по локальной сети (например, на переменах в школе).

## Особенности игры
* **Игра по локальной сети:** Не нужен интернет, достаточно подключить два компьютера к одной точке доступа Wi-Fi (можно раздать с телефона).
* **Классические правила:** Поле 8х8, шашки ходят по диагонали, при достижении края поля превращаются в дамки.
* **Система комбо:** Если одной шашкой можно срубить несколько фигур соперника подряд, игра не передает ход, а позволяет дорубить серию.
* **Легковесность:** Игра весит очень мало, не требует установки и плавно работает даже на старых ноутбуках.

## Как запустить и играть
1. Скачайте готовый файл `Checkers.exe` (или скомпилируйте исходный код из файла `CheckersForm.cs`, если хотите что-то изменить).
2. Подключите оба компьютера к одной Wi-Fi сети.
3. **Первый игрок (Сервер):** Нажимает кнопку «СОЗДАТЬ ИГРУ (LAN)». На экране отобразится IP-адрес.
4. **Второй игрок (Клиент):** Вводит IP-адрес первого игрока в текстовое поле и нажимает «ПОДКЛЮЧИТЬСЯ».
5. После успешного подключения можно начинать игру. Первыми ходят белые (Сервер).

## Технический стек
* **Язык:** C#
* **Платформа:** .NET Framework / Windows Forms
* **Сеть:** TCP/IP (TcpListener / TcpClient)

---

# Checkers LAN (English)

A simple network checkers game for two players, written in C# (Windows Forms). The game was created to easily play with a friend over a local network (for example, during school breaks).

## Features
* **LAN Multiplayer:** No internet required. It is enough to connect two computers to the same Wi-Fi hotspot (you can share it from your phone).
* **Classic Rules:** 8x8 board, checkers move diagonally and turn into kings when reaching the opposite edge of the board.
* **Combo System:** If a checker can capture multiple opponent pieces in a row, the game allows you to complete the entire combo before passing the turn.
* **Lightweight:** The game has a very small file size, requires no installation, and runs smoothly even on old laptops.

## How to Run and Play
1. Download the ready-to-run `Checkers.exe` file (or compile the source code from `CheckersForm.cs` if you want to modify it).
2. Connect both computers to the same Wi-Fi network.
3. **First Player (Host/Server):** Clicks the "СОЗДАТЬ ИГРУ (LAN)" button. The IP address will be displayed on the screen.
4. **Second Player (Client):** Enters the first player's IP address into the text field and clicks "ПОДКЛЮЧИТЬСЯ".
5. Once successfully connected, the game starts. White moves first (Host).

## Tech Stack
* **Language:** C#
* **Platform:** .NET Framework / Windows Forms
* **Networking:** TCP/IP (TcpListener / TcpClient)
