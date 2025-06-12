using System.IO.MemoryMappedFiles;

namespace playerSpace
{
    public partial class Form1 : Form
    {
        private const int GridSize = 10;
        private const int CellSize = 30;

        private Button[,] enemyButtons = new Button[GridSize, GridSize];

        private MemoryMappedFile mmfSpace;
        private MemoryMappedFile mmfShips;
        private MemoryMappedFile playerSpacee;
        private MemoryMappedFile playerShips;

        private EventWaitHandle eventSpace;
        private EventWaitHandle eventShips;
        private EventWaitHandle eventPlayerSpace;
        private EventWaitHandle eventPlayerShips;

        int score = 0;
        String[] turnText = ["ճ� ������", "��� ���"];
        bool turn = true;

        int x;
        int y;
        public Form1()
        {
            InitializeComponent();
            GenerateGameFields();
            this.Text = "������� 1";
            this.BackColor = Color.LightCyan;

            label1.Top = 380;
            label1.Left = 50;

            label2.Top = 20;
            label2.Left = 300;
            try
            {
                mmfSpace = MemoryMappedFile.OpenExisting("mmfSpace");
                mmfShips = MemoryMappedFile.OpenExisting("mmfShips");
                playerSpacee = MemoryMappedFile.OpenExisting("playerSpacee");
                playerShips = MemoryMappedFile.OpenExisting("playerShips");

                eventPlayerSpace = new EventWaitHandle(false, EventResetMode.AutoReset, "eventPlayerSpace");
                eventPlayerShips = new EventWaitHandle(false, EventResetMode.AutoReset, "eventPlayerShips");

                eventSpace = new EventWaitHandle(false, EventResetMode.AutoReset, "eventSpace");
                eventShips = new EventWaitHandle(false, EventResetMode.AutoReset, "eventShips");
                //����� ����������� ���
                Thread thread = new Thread(WaitForData);
                thread.IsBackground = true;
                thread.Start();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("���� ���'�� �� ��������!");
            }
        }
        string turnTextRecieved;
        private void WaitForData()
        {
            while (true)
            {
                eventSpace.WaitOne();
                using (MemoryMappedViewStream stream = mmfSpace.CreateViewStream())
                {
                    BinaryReader reader = new BinaryReader(stream);

                    turnTextRecieved = reader.ReadString();
                    turn = reader.ReadBoolean();

                    int row = reader.ReadInt32();
                    int col = reader.ReadInt32();

                    label1.Invoke(new Action(() => label1.Text = turnTextRecieved));
                    // ��� ���������
                    if (row != 11 && col != 11)
                    {
                        enemyButtons[row, col].BackColor = Color.Blue;
                        label1.Invoke(new Action(() => label1.Text = turnTextRecieved));
                        score++;
                        label2.Invoke(new Action(() => label2.Text = "score: " + score));
                        
                        //��������
                        if (score == 20) {
                            MessageBox.Show("��� ���������!");
                            turn = false;
                            label1.Invoke(new Action(() => label1.Text = "��� ��������!"));
                            using (MemoryMappedViewStream kill = playerShips.CreateViewStream()) {
                                BinaryWriter writer = new BinaryWriter(kill);
                                writer.Write(99);
                                writer.Write(99);                          
                            }
                            eventPlayerShips.Set();
                        }

                        //����������� ����
                        using (MemoryMappedViewStream stream2 = playerSpacee.CreateViewStream())
                        {
                            BinaryWriter bw = new BinaryWriter(stream2);

                            bw.Write(turnText[0]);
                            bw.Write(false);

                            bw.Write(11);
                            bw.Write(11);
                            label1.Invoke(new Action(() => label1.Text = turnText[1]));

                        }
                        eventPlayerSpace.Set();
                    }
                }
            }
        }

        private void GenerateGameFields()
        {
            int startX_Enemy = 50;
            int startY = 70;

            // ������ (����� A-J)
            char[] letters = "ABCDEFGHIJ".ToCharArray();


            // --- ���� ������������ ---
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Button btn = new Button();
                    btn.Width = CellSize;
                    btn.Height = CellSize;
                    btn.Left = startX_Enemy + col * CellSize;
                    btn.Top = startY + row * CellSize;
                    btn.Tag = new Point(row, col);
                    btn.BackColor = Color.LightGray;
                    btn.Click += EnemyCell_Click;
                    this.Controls.Add(btn);
                    enemyButtons[row, col] = btn;
                }
            }

            // --- ���������: ˳���� ������ (A-J) ---
            for (int col = 0; col < GridSize; col++)
            {

                // ��� ���� ������������
                Label lblEnemyCol = new Label();
                lblEnemyCol.Text = letters[col].ToString();
                lblEnemyCol.Left = startX_Enemy + col * CellSize + 10;
                lblEnemyCol.Top = startY - 20;
                lblEnemyCol.AutoSize = true;
                this.Controls.Add(lblEnemyCol);
            }

            // --- ���������: ����� ���� (1-10) ---
            for (int row = 0; row < GridSize; row++)
            {

                // ��� ���� ������������
                Label lblEnemyRow = new Label();
                lblEnemyRow.Text = (row + 1).ToString();
                lblEnemyRow.Left = startX_Enemy - 20;
                lblEnemyRow.Top = startY + row * CellSize + 5;
                lblEnemyRow.AutoSize = true;
                this.Controls.Add(lblEnemyRow);
            }

            Label lblEnemy = new Label();
            lblEnemy.Text = "���� ������������";
            lblEnemy.Left = startX_Enemy;
            lblEnemy.Top = startY - 50;
            lblEnemy.AutoSize = true;
            this.Controls.Add(lblEnemy);
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void EnemyCell_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                if (!(btn.BackColor == Color.Red || btn.BackColor == Color.Blue))
                    if (turn)
                    {
                        btn.BackColor = Color.Red;
                        Point p = (Point)btn.Tag;

                        x = (int)p.X;
                        y = (int)p.Y;
                        using (MemoryMappedViewStream stream = playerSpacee.CreateViewStream())
                        {

                            BinaryWriter bw = new BinaryWriter(stream);

                            bw.Write(turnText[1]);
                            bw.Write(true);

                            label1.Text = turnText[0];
                            turn = false;

                            bw.Write(11);
                            bw.Write(11);
                        }
                        eventPlayerSpace.Set();

                        using (MemoryMappedViewStream stream = mmfShips.CreateViewStream())
                        {
                            BinaryWriter writer = new BinaryWriter(stream);
                            writer.Write(x);
                            writer.Write(y);
                        }
                        eventShips.Set();
                    }
            }
        }

        
    }
}
