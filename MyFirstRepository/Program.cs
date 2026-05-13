using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new StartScreen());
    }
}

class StartScreen : Form
{
    private Button playButton;
    private Button instructionsButton;
    private Button quitButton;
    private Label titleLabel;

    public StartScreen()
    {
        this.Text = "Beat the Boss 4";
        this.Size = new Size(400, 300);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.DarkBlue;

        titleLabel = new Label();
        titleLabel.Text = "BEAT THE BOSS 4";
        titleLabel.Font = new Font("Arial", 24, FontStyle.Bold);
        titleLabel.ForeColor = Color.Yellow;
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(50, 30);
        this.Controls.Add(titleLabel);

        playButton = new Button();
        playButton.Text = "PLAY";
        playButton.Font = new Font("Arial", 14, FontStyle.Bold);
        playButton.Size = new Size(120, 40);
        playButton.Location = new Point(140, 100);
        playButton.BackColor = Color.Green;
        playButton.Click += PlayButton_Click;
        this.Controls.Add(playButton);

        instructionsButton = new Button();
        instructionsButton.Text = "INSTRUCTIONS";
        instructionsButton.Font = new Font("Arial", 12);
        instructionsButton.Size = new Size(120, 40);
        instructionsButton.Location = new Point(140, 150);
        instructionsButton.BackColor = Color.Orange;
        instructionsButton.Click += InstructionsButton_Click;
        this.Controls.Add(instructionsButton);

        quitButton = new Button();
        quitButton.Text = "QUIT";
        quitButton.Font = new Font("Arial", 12);
        quitButton.Size = new Size(120, 40);
        quitButton.Location = new Point(140, 200);
        quitButton.BackColor = Color.Red;
        quitButton.Click += QuitButton_Click;
        this.Controls.Add(quitButton);
    }

    private void PlayButton_Click(object sender, EventArgs e)
    {
        this.Hide();
        var gameForm = new GameForm();
        gameForm.ShowDialog();
        this.Show();
    }

    private void InstructionsButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            "Instructions:\n\n" +
            "- Use different weapons to defeat the boss\n" +
            "- The boss has health, you can reload and use special attacks\n" +
            "- Try to survive while the boss fights back\n\n" +
            "Controls:\n" +
            "- Click weapon buttons to attack\n" +
            "- Reload button to get more ammo\n" +
            "- Heal button to restore health",
            "Instructions",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void QuitButton_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
}

class GameForm : Form
{
    private Boss boss;
    private Player player;
    private List<Weapon> weapons;
    private Label bossHealthLabel;
    private Panel bossPanel;
    private Label bossInstructionLabel;
    private Label playerHealthLabel;
    private Label playerAmmoLabel;
    private Label playerMedkitsLabel;
    private Label statusLabel;
    private Button[] weaponButtons;
    private Button reloadButton;
    private Button healButton;
    private System.Windows.Forms.Timer gameTimer;
    private System.Windows.Forms.Timer bossMovementTimer;
    private System.Windows.Forms.Timer knifeAnimationTimer;
    private System.Windows.Forms.Timer hitAnimationTimer;
    private List<KnifeProjectile> activeKnives;
    private Random random;
    private int hitAnimationCounter;
    private int hitAnimationDuration;
    private int bossKnockbackX;
    private int bossKnockbackY;
    private int currentWeaponIndex = -1;
    private Dictionary<int, Cursor> weaponCursors;

    public GameForm()
    {
        this.Text = "Beat the Boss - Gameplay";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(18, 20, 28);
        this.KeyPreview = true;
        this.KeyDown += GameForm_KeyDown;

        InitializeGame();
        InitializeUI();
        StartGame();
    }

    private void InitializeGame()
    {
        random = new Random();
        boss = new Boss("Enemy Boss", 120);
        player = new Player(100, 8);
        activeKnives = new List<KnifeProjectile>();
        weaponCursors = new Dictionary<int, Cursor>();

        weapons = new List<Weapon>
        {
            new Weapon("Pistol", 12, 2, 5),
            new Weapon("Machine Gun", 20, 4, 3),
            new Weapon("Grenades", 35, 1, 1),
            new Weapon("Laser Pen", 50, 0, 1),
            new Weapon("Throwing Knives", 15, 3, 2)
        };

        // Create custom cursors for each weapon
        CreateWeaponCursors();
    }

    private void InitializeUI()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 3,
            RowCount = 2,
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        this.Controls.Add(mainLayout);

        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 32, 46),
            Padding = new Padding(20),
        };
        mainLayout.Controls.Add(leftPanel, 0, 0);

        var leftTitle = new Label
        {
            Text = "STATUS",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(8, 8),
        };
        leftPanel.Controls.Add(leftTitle);

        bossHealthLabel = CreateStatLabel($"Boss Health: {boss.Health}", 50);
        bossHealthLabel.Location = new Point(8, 60);
        leftPanel.Controls.Add(bossHealthLabel);

        playerHealthLabel = CreateStatLabel($"Player Health: {player.Health}", 110);
        leftPanel.Controls.Add(playerHealthLabel);

        playerAmmoLabel = CreateStatLabel($"Ammo: {player.Ammo}", 160);
        leftPanel.Controls.Add(playerAmmoLabel);

        playerMedkitsLabel = CreateStatLabel($"Medkits: {player.Medkits}", 210);
        leftPanel.Controls.Add(playerMedkitsLabel);

        var centerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(20),
        };
        mainLayout.Controls.Add(centerPanel, 1, 0);

        var centerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        centerPanel.Controls.Add(centerLayout);

        var bossTitle = new Label
        {
            Text = "THE BOSS",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 180, 60),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        centerLayout.Controls.Add(bossTitle, 0, 0);

        bossPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 26, 36),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(10),
        };
        bossPanel.Paint += BossPanel_Paint;
        bossPanel.Click += BossPanel_Click;
        bossPanel.MouseMove += BossPanel_MouseMove;
        bossPanel.MouseLeave += BossPanel_MouseLeave;
        centerLayout.Controls.Add(bossPanel, 0, 1);

        bossInstructionLabel = new Label
        {
            Text = "Click the boss to punch him!",
            Font = new Font("Segoe UI", 10, FontStyle.Italic),
            ForeColor = Color.LightGray,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        centerLayout.Controls.Add(bossInstructionLabel, 0, 2);

        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 32, 46),
            Padding = new Padding(20),
        };
        mainLayout.Controls.Add(rightPanel, 2, 0);

        var actionTitle = new Label
        {
            Text = "ACTIONS",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(8, 8),
        };
        rightPanel.Controls.Add(actionTitle);

        var actionLayout = new FlowLayoutPanel
        {
            Location = new Point(8, 60),
            Size = new Size(290, 260),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
        rightPanel.Controls.Add(actionLayout);

        weaponButtons = new Button[weapons.Count];
        for (int i = 0; i < weapons.Count; i++)
        {
            weaponButtons[i] = CreateActionButton($"{weapons[i].Name}    {weapons[i].DamageMin}-{weapons[i].DamageMax} dmg", 260);
            weaponButtons[i].Tag = i;
            weaponButtons[i].Click += WeaponButton_Click;
            actionLayout.Controls.Add(weaponButtons[i]);
        }

        reloadButton = CreateActionButton("RELOAD (+5 ammo)", 260, Color.FromArgb(70, 130, 255));
        reloadButton.Click += ReloadButton_Click;
        actionLayout.Controls.Add(reloadButton);

        healButton = CreateActionButton("HEAL (+20 health)", 260, Color.FromArgb(170, 50, 210));
        healButton.Click += HealButton_Click;
        actionLayout.Controls.Add(healButton);

        statusLabel = new Label
        {
            Text = "Choose your action!",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(12, 16, 26),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        mainLayout.Controls.Add(statusLabel, 0, 1);
        mainLayout.SetColumnSpan(statusLabel, 3);

        gameTimer = new System.Windows.Forms.Timer();
        gameTimer.Interval = 2000; // 2 seconds
        gameTimer.Tick += GameTimer_Tick;

        bossMovementTimer = new System.Windows.Forms.Timer();
        bossMovementTimer.Interval = 100; // 10 times per second
        bossMovementTimer.Tick += BossMovementTimer_Tick;

        knifeAnimationTimer = new System.Windows.Forms.Timer();
        knifeAnimationTimer.Interval = 50; // 20 times per second
        knifeAnimationTimer.Tick += KnifeAnimationTimer_Tick;

        hitAnimationTimer = new System.Windows.Forms.Timer();
        hitAnimationTimer.Interval = 30; // Hit animation timing
        hitAnimationTimer.Tick += HitAnimationTimer_Tick;
    }

    private Label CreateStatLabel(string text, int top)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(8, top),
        };
    }

    private Button CreateActionButton(string text, int width, Color? backColor = null)
    {
        var button = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Size = new Size(width, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor ?? Color.FromArgb(58, 62, 76),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6),
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void GameForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            this.Close();
        }
    }

    private void StartGame()
    {
        gameTimer.Start();
        bossMovementTimer.Start();
        knifeAnimationTimer.Start();
        hitAnimationTimer.Start();
        UpdateUI();
    }

    private void UpdateUI()
    {
        bossHealthLabel.Text = $"Health: {boss.Health}";
        playerHealthLabel.Text = $"Health: {player.Health}";
        playerAmmoLabel.Text = $"Ammo: {player.Ammo}";
        playerMedkitsLabel.Text = $"Medkits: {player.Medkits}";

        // Update weapon button colors based on ammo
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            var enabled = player.Ammo >= weapons[i].AmmoCost;
            weaponButtons[i].Enabled = enabled;
            weaponButtons[i].BackColor = enabled ? Color.FromArgb(58, 62, 76) : Color.FromArgb(40, 44, 56);
        }

        var healEnabled = player.Medkits > 0;
        healButton.Enabled = healEnabled;
        healButton.BackColor = healEnabled ? Color.FromArgb(170, 50, 210) : Color.FromArgb(40, 44, 56);
    }

    private void WeaponButton_Click(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var weaponIndex = (int)button.Tag;
        var weapon = weapons[weaponIndex];
        
        // Set current weapon
        currentWeaponIndex = weaponIndex;

        if (player.Ammo >= weapon.AmmoCost)
        {
            player.Ammo -= weapon.AmmoCost;

            if (weapon.Name == "Throwing Knives")
            {
                // Create knife projectiles
                for (int i = 0; i < 3; i++)
                {
                    var knife = new KnifeProjectile(boss.X, boss.Y, random);
                    activeKnives.Add(knife);
                }
                statusLabel.Text = "You threw 3 knives at the boss!";
            }
            else
            {
                var damage = weapon.DealDamage();
                boss.TakeDamage(damage);
                TriggerHitAnimation();
                statusLabel.Text = $"You used {weapon.Name} and dealt {damage} damage to the boss!";
            }

            UpdateUI();
            bossPanel.Invalidate();

            if (!boss.IsAlive)
            {
                EndGame(true);
            }
        }
    }

    private void BossPanel_MouseMove(object sender, MouseEventArgs e)
    {
        // Change cursor based on selected weapon
        if (currentWeaponIndex >= 0 && weaponCursors.ContainsKey(currentWeaponIndex))
        {
            bossPanel.Cursor = weaponCursors[currentWeaponIndex];
        }
        else
        {
            bossPanel.Cursor = Cursors.Default;
        }
    }

    private void BossPanel_MouseLeave(object sender, EventArgs e)
    {
        // Restore default cursor
        bossPanel.Cursor = Cursors.Default;
    }

    private void BossPanel_Click(object sender, EventArgs e)
    {
        if (player.IsAlive && boss.IsAlive)
        {
            var damage = 8;
            boss.TakeDamage(damage);
            statusLabel.Text = $"You punched the boss and dealt {damage} damage!";
            
            // Start hit animation
            TriggerHitAnimation();
            
            UpdateUI();
            bossPanel.Invalidate();

            if (!boss.IsAlive)
            {
                EndGame(true);
            }
        }
    }

    private void BossPanel_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (boss.IsAlive)
        {
            // Get draw position with knockback
            int drawX = boss.X + bossKnockbackX;
            int drawY = boss.Y + bossKnockbackY;

            // Determine boss color based on hit animation
            var bodyBrush = Brushes.DarkGray;
            var headBrush = Brushes.LightGray;
            if (hitAnimationCounter > 0 && hitAnimationCounter % 2 == 0)
            {
                // Flash white when hit
                bodyBrush = Brushes.White;
                headBrush = Brushes.White;
            }

            // Draw boss with realistic outfit
            // Body (armor)
            g.FillRectangle(bodyBrush, drawX - 20, drawY - 15, 40, 30);
            g.DrawRectangle(Pens.Black, drawX - 20, drawY - 15, 40, 30);

            // Head
            g.FillEllipse(headBrush, drawX - 10, drawY - 35, 20, 20);
            g.DrawEllipse(Pens.Black, drawX - 10, drawY - 35, 20, 20);

            // Eyes
            g.FillEllipse(Brushes.Red, drawX - 7, drawY - 32, 4, 4);
            g.FillEllipse(Brushes.Red, drawX - 1, drawY - 32, 4, 4);

            // Armor details
            g.DrawLine(Pens.Black, drawX - 15, drawY - 10, drawX + 15, drawY - 10);
            g.DrawLine(Pens.Black, drawX - 15, drawY + 5, drawX + 15, drawY + 5);

            // Arms
            g.FillRectangle(Brushes.LightGray, drawX - 30, drawY - 10, 10, 20);
            g.FillRectangle(Brushes.LightGray, drawX + 20, drawY - 10, 10, 20);
            g.DrawRectangle(Pens.Black, drawX - 30, drawY - 10, 10, 20);
            g.DrawRectangle(Pens.Black, drawX + 20, drawY - 10, 10, 20);

            // Legs
            g.FillRectangle(Brushes.DarkGray, drawX - 12, drawY + 15, 8, 15);
            g.FillRectangle(Brushes.DarkGray, drawX + 4, drawY + 15, 8, 15);
            g.DrawRectangle(Pens.Black, drawX - 12, drawY + 15, 8, 15);
            g.DrawRectangle(Pens.Black, drawX + 4, drawY + 15, 8, 15);
        }

        // Draw knife projectiles
        foreach (var knife in activeKnives)
        {
            if (!knife.HasHit)
            {
                // Draw knife as a small rectangle with handle
                g.FillRectangle(Brushes.Silver, knife.X - 3, knife.Y - 8, 6, 16);
                g.FillRectangle(Brushes.Brown, knife.X - 1, knife.Y + 8, 2, 4);
                g.DrawRectangle(Pens.Black, knife.X - 3, knife.Y - 8, 6, 16);
                g.DrawRectangle(Pens.Black, knife.X - 1, knife.Y + 8, 2, 4);
            }
        }
    }

    private void ReloadButton_Click(object sender, EventArgs e)
    {
        player.Reload();
        statusLabel.Text = "Reloaded! +5 ammo";
        UpdateUI();
    }

    private void HealButton_Click(object sender, EventArgs e)
    {
        var healAmount = player.Heal();
        if (healAmount > 0)
        {
            statusLabel.Text = $"Healed! +{healAmount} health";
            UpdateUI();
        }
    }

    private void BossMovementTimer_Tick(object sender, EventArgs e)
    {
        if (boss.IsAlive)
        {
            boss.UpdatePosition();
            bossPanel.Invalidate();
        }
    }

    private void KnifeAnimationTimer_Tick(object sender, EventArgs e)
    {
        for (int i = activeKnives.Count - 1; i >= 0; i--)
        {
            var knife = activeKnives[i];
            knife.Update();

            // Check collision with boss
            var bossRect = new Rectangle(boss.X - 25, boss.Y - 25, 50, 50);
            var knifeRect = new Rectangle(knife.X - 5, knife.Y - 5, 10, 10);

            if (bossRect.IntersectsWith(knifeRect) && !knife.HasHit)
            {
                knife.HasHit = true;
                var damage = random.Next(5, 16); // 5-15 damage per knife
                boss.TakeDamage(damage);
                TriggerHitAnimation();
                statusLabel.Text = $"Knife hit! Dealt {damage} damage to the boss!";
                UpdateUI();
            }

            if (knife.IsExpired)
            {
                activeKnives.RemoveAt(i);
            }
        }

        if (activeKnives.Count > 0)
        {
            bossPanel.Invalidate();
        }

        if (!boss.IsAlive)
        {
            EndGame(true);
        }
    }

    private void GameTimer_Tick(object sender, EventArgs e)
    {
        if (boss.IsAlive && player.IsAlive)
        {
            var bossDamage = boss.Attack();
            player.TakeDamage(bossDamage);
            statusLabel.Text = $"Boss attacks! You took {bossDamage} damage!";

            UpdateUI();

            if (!player.IsAlive)
            {
                EndGame(false);
            }
        }
    }

    private void HitAnimationTimer_Tick(object sender, EventArgs e)
    {
        if (hitAnimationCounter > 0)
        {
            hitAnimationCounter--;

            // Reduce knockback over time
            if (bossKnockbackX != 0)
            {
                bossKnockbackX = (int)(bossKnockbackX * 0.8);
            }
            if (bossKnockbackY != 0)
            {
                bossKnockbackY = (int)(bossKnockbackY * 0.8);
            }

            bossPanel.Invalidate();
        }
    }

    private void TriggerHitAnimation()
    {
        hitAnimationCounter = 10; // Animate for 10 frames
        bossKnockbackX = -15; // Knockback to the left
        bossKnockbackY = -10; // Knockback upward
    }

    private void CreateWeaponCursors()
    {
        // Pistol cursor - crosshair-like
        weaponCursors[0] = CreateBitmapCursor("🔫", Color.Yellow);

        // Machine Gun cursor
        weaponCursors[1] = CreateBitmapCursor("🔫", Color.Orange);

        // Grenades cursor
        weaponCursors[2] = CreateBitmapCursor("💣", Color.Red);

        // Laser Pen cursor
        weaponCursors[3] = CreateBitmapCursor("🔴", Color.LimeGreen);

        // Throwing Knives cursor - knife
        weaponCursors[4] = CreateBitmapCursor("🔪", Color.Silver);
    }

    private Cursor CreateBitmapCursor(string symbol, Color color)
    {
        try
        {
            // Create a 32x32 bitmap for the cursor
            Bitmap cursorBitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(cursorBitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw a colored circle/shape
                using (var brush = new SolidBrush(color))
                {
                    // Draw outer circle
                    g.FillEllipse(brush, 8, 8, 16, 16);
                }

                // Draw crosshair or targeting lines
                using (var pen = new Pen(Color.White, 1.5f))
                {
                    g.DrawLine(pen, 16, 4, 16, 28);  // Vertical line
                    g.DrawLine(pen, 4, 16, 28, 16);  // Horizontal line
                }
            }

            // Convert bitmap to cursor
            IntPtr ptr = cursorBitmap.GetHicon();
            Cursor cursor = new Cursor(ptr);
            return cursor;
        }
        catch
        {
            // Fallback to default cursor if bitmap creation fails
            return Cursors.Default;
        }
    }

    private void EndGame(bool playerWon)
    {
        gameTimer.Stop();
        bossMovementTimer.Stop();
        knifeAnimationTimer.Stop();
        hitAnimationTimer.Stop();
        activeKnives.Clear();

        string message = playerWon
            ? "Congratulations! You defeated the boss!"
            : "Game Over! The boss defeated you.";

        var result = MessageBox.Show(
            message + "\n\nPlay again?",
            "Game Over",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            InitializeGame();
            StartGame();
        }
        else
        {
            this.Close();
        }
    }
}

class Player
{
    public int Health { get; private set; }
    public int Ammo { get; set; }
    public int Medkits { get; private set; }

    public bool IsAlive => Health > 0;

    public Player(int health, int ammo)
    {
        Health = health;
        Ammo = ammo;
        Medkits = 2;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public void Reload()
    {
        Ammo += 5;
    }

    public int Heal()
    {
        if (Medkits <= 0)
        {
            return 0;
        }

        Medkits--;
        var healAmount = 20;
        Health += healAmount;
        if (Health > 100) Health = 100;
        return healAmount;
    }
}

class Boss
{
    public string Name { get; }
    public int Health { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }

    public bool IsAlive => Health > 0;
    private readonly Random _random = new();

    public Boss(string name, int health)
    {
        Name = name;
        Health = health;
        // Start in center of boss panel
        X = 120;
        Y = 120;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public int Attack()
    {
        var baseDamage = _random.Next(6, 13);
        if (Health < 40)
        {
            // Desperate final attack
            return baseDamage + 5;
        }
        return baseDamage;
    }

    public void UpdatePosition()
    {
        // Move randomly within bounds
        int moveX = _random.Next(-10, 11); // -10 to +10
        int moveY = _random.Next(-10, 11); // -10 to +10

        X += moveX;
        Y += moveY;

        // Keep within panel bounds (boss panel is roughly 240x240)
        X = Math.Max(40, Math.Min(200, X));
        Y = Math.Max(40, Math.Min(200, Y));
    }
}

class Weapon
{
    public string Name { get; }
    public int DamageMin { get; }
    public int DamageMax { get; }
    public int AmmoCost { get; }
    public int Accuracy { get; }

    private readonly Random _random = new();

    public Weapon(string name, int damageMax, int accuracy, int ammoCost)
    {
        Name = name;
        DamageMin = Math.Max(1, damageMax / 2);
        DamageMax = damageMax;
        Accuracy = accuracy;
        AmmoCost = ammoCost;
    }

    public int DealDamage()
    {
        if (_random.Next(0, 10) < Accuracy)
        {
            return _random.Next(DamageMin, DamageMax + 1);
        }
        return _random.Next(1, DamageMin + 1);
    }
}

class KnifeProjectile
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool HasHit { get; set; }
    public bool IsExpired => HasHit || Y < 0;

    private readonly int _targetX;
    private readonly int _targetY;
    private readonly Random _random;

    public KnifeProjectile(int targetX, int targetY, Random random)
    {
        _random = random;
        _targetX = targetX;
        _targetY = targetY;

        // Start from bottom of screen with some randomness
        X = _random.Next(50, 190);
        Y = 220;

        HasHit = false;
    }

    public void Update()
    {
        if (!HasHit)
        {
            // Move towards target with some arc
            int dx = _targetX - X;
            int dy = _targetY - Y;

            // Normalize direction
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > 0)
            {
                double speed = 8.0;
                X += (int)(dx / distance * speed);
                Y += (int)(dy / distance * speed);
            }

            // Add some randomness to make it less predictable
            X += _random.Next(-2, 3);
            Y += _random.Next(-2, 3);
        }
    }
}
