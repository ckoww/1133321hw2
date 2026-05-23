using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _1133321hw2
{
    public partial class gaming : Form
    {
        List<Image> images = new List<Image>();
        Random rand = new Random();

        PictureBox firstCard = null;
        PictureBox secondCard = null;
             
        int hintCount = 1;        // 偷看的次數

        bool lockClick = false;   // 翻回等待時禁止點擊
        int timeCount = 0;        // 計時        

        public gaming()
        {
            InitializeComponent();
            LoadImages();
            RestartGame();
        }
        // 載入圖片（不包含背面）
        private void LoadImages()
        {
            images.Add(Properties.Resources._8_1);
            images.Add(Properties.Resources._8_2);
            images.Add(Properties.Resources._8_3);
            images.Add(Properties.Resources._8_4);
            images.Add(Properties.Resources._8_5);
            images.Add(Properties.Resources._8_6);
            images.Add(Properties.Resources._8_7);
            images.Add(Properties.Resources._8_8);
        }
        private void RestartGame()
        {
            // 播放重新開始的音效
            System.Media.SoundPlayer startPlayer = new System.Media.SoundPlayer(Properties.Resources.start);
            startPlayer.Play();
            timeCount = 0;
            lblTime.Text = "PLAY!";
            //重置偷看次數與按鈕狀態
            hintCount = 1;
            btnHint.Enabled = true;

            tmrPlayTime.Start();

            // 卡片池：8 種 × 2
            List<Image> cards = new List<Image>();
            foreach (var img in images)
            {
                cards.Add(img);
                cards.Add(img);
            }

            // 隨機排列
            cards = cards.OrderBy(x => rand.Next()).ToList();

            int idx = 0;
            foreach (var pic in this.Controls.OfType<PictureBox>())
            {
                // 設定背面圖片
                pic.Image = Properties.Resources._8_0;

                // Tag 存 Tuple<Image, bool> → Item1 = 卡片, Item2 = 是否翻開
                pic.Tag = new Tuple<Image, bool>(cards[idx], false);
                pic.Enabled = true;

                pic.Click -= picCell_1_1_Click;
                pic.Click += picCell_1_1_Click;

                //加黑框
                pic.BorderStyle = BorderStyle.FixedSingle;

                idx++;
            }

            firstCard = null;
            secondCard = null;
            lockClick = false;
        }

        private void mnuRestart_Click(object sender, EventArgs e)
        {
            RestartGame();
        }



        private void picCell_1_1_Click(object sender, EventArgs e)
        {
            if (lockClick) return;

            PictureBox pic = (PictureBox)sender;

            var t = (Tuple<Image, bool>)pic.Tag;

            // 已翻開 → 不動作
            if (t.Item2) return;

            // 翻牌
            pic.Image = t.Item1;
            pic.Tag = new Tuple<Image, bool>(t.Item1, true);

            if (firstCard == null)
            {
                firstCard = pic;
                return;
            }

            secondCard = pic;

            // 判斷配對
            var firstImg = ((Tuple<Image, bool>)firstCard.Tag).Item1;
            var secondImg = ((Tuple<Image, bool>)secondCard.Tag).Item1;

            if (firstImg == secondImg)
            {
                // 配對成功
                firstCard = null;
                secondCard = null;
                CheckWin();
            }
            else
            {
                // 配對失敗 → 翻回
                lockClick = true;
                tmrDelay.Start();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timeCount++;
            lblTime.Text = $"{timeCount} Seconds";
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            tmrDelay.Stop();

            // 翻回第一張
            var firstTag = (Tuple<Image, bool>)firstCard.Tag;
            firstCard.Image = Properties.Resources._8_0;
            firstCard.Tag = new Tuple<Image, bool>(firstTag.Item1, false);

            // 翻回第二張
            var secondTag = (Tuple<Image, bool>)secondCard.Tag;
            secondCard.Image = Properties.Resources._8_0;
            secondCard.Tag = new Tuple<Image, bool>(secondTag.Item1, false);

            firstCard = null;
            secondCard = null;
            lockClick = false; // 玩家可以再次點擊
        }

        // 判斷是否全部配對完成
        private void CheckWin()
        {
            bool allMatched = true;
            foreach (var pic in this.Controls.OfType<PictureBox>())
            {
                var t = (Tuple<Image, bool>)pic.Tag;
                if (!t.Item2) // 有未翻開的卡片
                {
                    allMatched = false;
                    break;
                }
            }

            if (allMatched)
            {
                //停止遊戲計時器
                tmrPlayTime.Stop();
                // 播放過關音效
                SoundPlayer winPlayer = new SoundPlayer(Properties.Resources.win); // 替換成你的資源名稱
                winPlayer.Play();

                //跳出恭喜完成的對話框，並顯示花費時間
                string message = $"恭喜過關！\n你總共花了 {timeCount} 秒完成挑戰！";
                string title = "遊戲完成";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void timerHint_Tick(object sender, EventArgs e)
        {
            tmrHint.Stop();

            // 將所有「未配對成功」的牌翻回背面
            foreach (var pic in this.Controls.OfType<PictureBox>())
            {
                var t = (Tuple<Image, bool>)pic.Tag;
                if (!t.Item2) // 如果狀態是未翻開 (false)
                {
                    pic.Image = Properties.Resources._8_0; // 蓋回背面
                }
            }

            // 解除鎖定，讓玩家可以繼續玩
            lockClick = false;
        }

        private void btnHint_Click(object sender, EventArgs e)
        {
            // 如果次數用完、畫面鎖定中、或是已經翻了第一張牌，就不執行
            if (hintCount <= 0 || lockClick || firstCard != null) return;
            // 播放提示音效
            SoundPlayer hintPlayer = new SoundPlayer(Properties.Resources.hint); // 替換成你的資源名稱
            hintPlayer.Play();

            hintCount--;            
            btnHint.Enabled = false;

            // 鎖定點擊，避免偷看時玩家亂點卡片造成邏輯錯誤
            lockClick = true;

            // 將所有「未配對成功」的牌顯示正面
            foreach (var pic in this.Controls.OfType<PictureBox>())
            {
                var t = (Tuple<Image, bool>)pic.Tag;
                if (!t.Item2) // 如果狀態是未翻開 (false)
                {
                    pic.Image = t.Item1; // 顯示正面圖片 (但不改變 Tag 的狀態)
                }
            }
            tmrHint.Start();
        }
    }
}
