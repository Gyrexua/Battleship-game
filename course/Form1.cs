using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using static System.Windows.Forms.AxHost;

namespace course
{
    public partial class Form1 : Form
    {
        private List<Process> childProcesses = new List<Process>();

        private const int GridSize = 10;
        private const int CellSize = 30;

        private Button[,] playerButtons = new Button[GridSize, GridSize];

        private MemoryMappedFile mmfSpace;
        private MemoryMappedFile mmfShips;
        private MemoryMappedFile playerSpacee;
        private MemoryMappedFile playerShips;

        private MemoryMappedFile turns;


        private EventWaitHandle eventSpace;
        private EventWaitHandle eventShips;
        private EventWaitHandle eventPlayerSpace;
        private EventWaitHandle eventPlayerShips;

        Process playerSpace = new Process();
        Process enemySpace = new Process();
        Process enemyShips = Process.Start("C:\\Users\\Gyrex\\OneDrive\\Desktop\\уник\\C#\\lab8\\course\\enemyShips\\bin\\Debug\\net8.0-windows\\enemyShips.exe");

        int row;
        int col; 
        
        String[] turnText = ["Ход ворога", "Ваш хід"];

        public Form1()
        {
            InitializeComponent();
            GenerateGameFields();
            generateShips();
            this.Text = "Гравець 1";
            this.BackColor = Color.LightCyan;
            button3.Enabled = false;

            button1.Top = 390;
            button1.Left = 50;

            button2.Top = 380;
            button2.Left = 150;

            button3.Top = 380;
            button3.Left = 300;
            //Процессы
            mmfSpace = MemoryMappedFile.CreateOrOpen("mmfSpace", 1024);
            mmfShips = MemoryMappedFile.CreateOrOpen("mmfShips", 1024);
            playerSpacee = MemoryMappedFile.CreateOrOpen("playerSpacee", 1024);
            playerShips = MemoryMappedFile.CreateOrOpen("playerShips", 1024);
            turns = MemoryMappedFile.CreateOrOpen("turns", 1024);



            eventSpace = new EventWaitHandle(false, EventResetMode.AutoReset, "eventSpace");
            eventShips = new EventWaitHandle(false, EventResetMode.AutoReset, "eventShips");

            eventPlayerSpace = new EventWaitHandle(false, EventResetMode.AutoReset, "eventPlayerSpace");
            eventPlayerShips = new EventWaitHandle(false, EventResetMode.AutoReset, "eventPlayerShips");



            playerSpace.StartInfo.FileName = "C:\\Users\\Gyrex\\OneDrive\\Desktop\\уник\\C#\\lab8\\course\\playerSpace\\bin\\Debug\\net8.0-windows\\playerSpace.exe";
            enemySpace.StartInfo.FileName = "C:\\Users\\Gyrex\\OneDrive\\Desktop\\уник\\C#\\lab8\\course\\enemySpace\\bin\\Debug\\net8.0-windows\\enemySpace.exe";

            enemySpace.StartInfo.UseShellExecute = false;
            enemyShips.StartInfo.UseShellExecute = false;
            playerSpace.StartInfo.UseShellExecute = false;

            //поток принимайщий ход
            Thread thread = new Thread(WaitForData);
            thread.IsBackground = true;
            thread.Start();

            childProcesses.Add(playerSpace);
            childProcesses.Add(enemySpace);
            childProcesses.Add(enemyShips);


            this.FormClosed += MainForm_FormClosed;

        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (var proc in childProcesses)
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void WaitForData()
        {
            while (true)
            {
                eventPlayerShips.WaitOne();
                using (MemoryMappedViewStream stream = playerShips.CreateViewStream())
                {
                    BinaryReader reader = new BinaryReader(stream);

                    row = reader.ReadInt32();
                    col = reader.ReadInt32();


                    if (row == 99 && col == 99)
                    {
                        playerSpace.Kill();
                        enemySpace.Kill();
                        clearShips();
                        generateShips();

                        button1.Invoke(new Action(() => button1.Enabled = true));
                        button2.Invoke(new Action(() => button2.Enabled = true));
                        button3.Invoke(new Action(() => button3.Enabled = false));


                        using (MemoryMappedViewStream restart = mmfShips.CreateViewStream())
                        {
                            BinaryWriter writer = new BinaryWriter(restart);
                            writer.Write(99);
                            writer.Write(99);
                        }
                        eventShips.Set();
                    }
                    else {
                        EnemyShot(row, col);
                    }
                }
            }
        }
        private void EnemyShot(int row, int col)
        {
            if (playerButtons[row, col].BackColor == Color.Green)
            {
                playerButtons[row, col].BackColor = Color.Red;
                playerButtons[row, col].Invoke(new Action(() => playerButtons[row, col].Text = "X"));
                using (MemoryMappedViewStream stream = playerSpacee.CreateViewStream())
                {
                    BinaryWriter bw = new BinaryWriter(stream);
                    bw.Write(turnText[1]);
                    bw.Write(true);

                    bw.Write(row);
                    bw.Write(col);
                }
                eventPlayerSpace.Set();
            }
            else playerButtons[row, col].BackColor = Color.Red;

        }
        private void GenerateGameFields()
        {
            int startX_Player = 50;
            int startY = 70;
            int startX_Enemy = startX_Player + (CellSize * GridSize) + 150;

            // Алфавіт (букви A-J)
            char[] letters = "ABCDEFGHIJ".ToCharArray();

            // --- Поле гравця ---
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Button btn = new Button();
                    btn.Width = CellSize;
                    btn.Height = CellSize;
                    btn.Left = startX_Player + col * CellSize;
                    btn.Top = startY + row * CellSize;
                    btn.Tag = new Point(row, col);
                    btn.BackColor = Color.LightBlue;
                    this.Controls.Add(btn);
                    playerButtons[row, col] = btn;
                }
            }



            // --- Нумерація: Літера зверху (A-J) ---
            for (int col = 0; col < GridSize; col++)
            {
                // Для поля гравця
                Label lblPlayerCol = new Label();
                lblPlayerCol.Text = letters[col].ToString();
                lblPlayerCol.Left = startX_Player + col * CellSize + 10;
                lblPlayerCol.Top = startY - 20;
                lblPlayerCol.AutoSize = true;
                this.Controls.Add(lblPlayerCol);
            }


            // --- Нумерація: Цифра зліва (1-10) ---
            for (int row = 0; row < GridSize; row++)
            {
                // Для поля гравця
                Label lblPlayerRow = new Label();
                lblPlayerRow.Text = (row + 1).ToString();
                lblPlayerRow.Left = startX_Player - 20;
                lblPlayerRow.Top = startY + row * CellSize + 5;
                lblPlayerRow.AutoSize = true;
                this.Controls.Add(lblPlayerRow);

            }

            // --- Назви полів ---
            Label lblPlayer = new Label();
            lblPlayer.Text = "Гравець 1";
            lblPlayer.Left = startX_Player;
            lblPlayer.Top = startY - 50;
            lblPlayer.AutoSize = true;
            this.Controls.Add(lblPlayer);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        //оновити
        private void button1_Click(object sender, EventArgs e)
        {
            clearShips();
            generateShips();
        }
        private void generateShips()
        {
            Random colRand = new Random();
            Random rowRand = new Random();
            Random posRand = new Random();

            //playerButtons[rowRand.Next(10), colRand.Next(10)]
            int[] shipAmount = [0, 4, 3, 2, 1];
            //4 ship
            while (true)
            {

                bool canPlace = true;
                int pos = posRand.Next(2);

                if (pos == 0)
                {
                    int row = colRand.Next(10);
                    int col = colRand.Next(7);
                    for (int r = row - 1; r <= row + 1; r++)
                    {
                        for (int c = col - 1; c <= col + 4; c++)
                        {
                            if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                            if (playerButtons[r, c].BackColor == Color.Green)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (!canPlace) break;
                    }
                    if (canPlace)
                    {
                        playerButtons[row, col].BackColor = Color.Green;
                        playerButtons[row, col + 1].BackColor = Color.Green;
                        playerButtons[row, col + 2].BackColor = Color.Green;
                        playerButtons[row, col + 3].BackColor = Color.Green;
                        break;
                    }
                }
                if (pos == 1)
                {
                    int row = colRand.Next(7);
                    int col = colRand.Next(10);
                    for (int r = row - 1; r <= row + 4; r++)
                    {
                        for (int c = col - 1; c <= col + 1; c++)
                        {
                            if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                            if (playerButtons[r, c].BackColor == Color.Green)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (!canPlace) break;
                    }
                    if (canPlace)
                    {
                        playerButtons[row, col].BackColor = Color.Green;
                        playerButtons[row + 1, col].BackColor = Color.Green;
                        playerButtons[row + 2, col].BackColor = Color.Green;
                        playerButtons[row + 3, col].BackColor = Color.Green;
                        break;
                    }
                }
            }
            // 3 ship
            for (int i = 0; i < shipAmount[3]; i++)
            {
                while (true)
                {
                    bool canPlace = true;
                    int pos = posRand.Next(2);

                    if (pos == 0)
                    {
                        int row = colRand.Next(10);
                        int col = colRand.Next(8);
                        for (int r = row - 1; r <= row + 1; r++)
                        {
                            for (int c = col - 1; c <= col + 3; c++)
                            {
                                if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                                if (playerButtons[r, c].BackColor == Color.Green)
                                {
                                    canPlace = false;
                                    break;
                                }
                            }
                            if (!canPlace) break;
                        }
                        if (canPlace)
                        {
                            playerButtons[row, col].BackColor = Color.Green;
                            playerButtons[row, col + 1].BackColor = Color.Green;
                            playerButtons[row, col + 2].BackColor = Color.Green;
                            break;
                        }
                    }
                    if (pos == 1)
                    {
                        int row = colRand.Next(8);
                        int col = colRand.Next(10);
                        for (int r = row - 1; r <= row + 3; r++)
                        {
                            for (int c = col - 1; c <= col + 1; c++)
                            {
                                if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                                if (playerButtons[r, c].BackColor == Color.Green)
                                {
                                    canPlace = false;
                                    break;
                                }
                            }
                            if (!canPlace) break;
                        }
                        if (canPlace)
                        {
                            playerButtons[row, col].BackColor = Color.Green;
                            playerButtons[row + 1, col].BackColor = Color.Green;
                            playerButtons[row + 2, col].BackColor = Color.Green;
                            break;
                        }
                    }
                }
            }
            // 2 ship
            for (int i = 0; i < shipAmount[2]; i++)
            {
                while (true)
                {

                    bool canPlace = true;
                    int pos = posRand.Next(2);

                    if (pos == 0)
                    {
                        int row = colRand.Next(10);
                        int col = colRand.Next(9);
                        for (int r = row - 1; r <= row + 1; r++)
                        {
                            for (int c = col - 1; c <= col + 2; c++)
                            {
                                if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                                if (playerButtons[r, c].BackColor == Color.Green)
                                {
                                    canPlace = false;
                                    break;
                                }
                            }
                            if (!canPlace) break;
                        }
                        if (canPlace)
                        {
                            playerButtons[row, col].BackColor = Color.Green;
                            playerButtons[row, col + 1].BackColor = Color.Green;
                            break;
                        }
                    }
                    if (pos == 1)
                    {
                        int row = colRand.Next(9);
                        int col = colRand.Next(10);
                        for (int r = row - 1; r <= row + 2; r++)
                        {
                            for (int c = col - 1; c <= col + 1; c++)
                            {
                                if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                                if (playerButtons[r, c].BackColor == Color.Green)
                                {
                                    canPlace = false;
                                    break;
                                }
                            }
                            if (!canPlace) break;
                        }
                        if (canPlace)
                        {
                            playerButtons[row, col].BackColor = Color.Green;
                            playerButtons[row + 1, col].BackColor = Color.Green;
                            break;
                        }
                    }
                }
            }
            //

            // 1 ship
            for (int i = 0; i < shipAmount[1]; i++)
            {
                while (true)
                {

                    int row = colRand.Next(10);
                    int col = colRand.Next(10);
                    bool canPlace = true;

                    for (int r = row - 1; r <= row + 1; r++)
                    {
                        for (int c = col - 1; c <= col + 1; c++)
                        {
                            if (r < 0 || r >= 10 || c < 0 || c >= 10) continue;
                            if (playerButtons[r, c].BackColor == Color.Green)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (!canPlace) break;
                    }
                    if (canPlace)
                    {
                        playerButtons[row, col].BackColor = Color.Green;
                        break;
                    }
                }
            }
        }

        private void clearShips()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    playerButtons[row, col].BackColor = Color.LightBlue;
                    playerButtons[row, col].Invoke(new Action(() => playerButtons[row, col].Text = ""));
                }
            }

        }
        //почати гру
        private void button2_Click(object sender, EventArgs e)
        {
            enemySpace.Start();
            playerSpace.Start();

            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = true;
            using (MemoryMappedViewStream stream = mmfShips.CreateViewStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(13);
                writer.Write(13);
            }
            eventShips.Set();
        }
        //закінчити гру
        private void button3_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;

            clearShips();
            generateShips();

            enemySpace.Kill();
            playerSpace.Kill();

            using (MemoryMappedViewStream stream = mmfShips.CreateViewStream()) { 
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(12);
                writer.Write(12);
            }
            eventShips.Set();
        }
    }
}
