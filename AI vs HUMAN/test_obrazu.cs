using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
namespace AI_vs_HUMAN
{
    public partial class test_obrazu : Form
    {
        private Size originalSize;
        private Dictionary<Control, Rectangle> originalControlBounds = new Dictionary<Control, Rectangle>();
        private int result_from_model = -1;
        public test_obrazu()
        {
            InitializeComponent();
            this.Shown += (s, e) =>
            {
                this.WindowState = FormWindowState.Maximized;
            };

            this.Load += testLoad;
            this.Resize += testResize;
        }
        private void testLoad(object sender, EventArgs e)
        {
            originalSize = this.Size;
            StoreOriginalBoundsRecursive(this);
        }
        private void testResize(object sender, EventArgs E)
        {
            ResizeControlsRecursive(this);
            //float scaleX = ((float)this.Width / originalSize.Width);
            //float scaleY = ((float)this.Height / originalSize.Height);
            //this.Scale(new SizeF(scaleX, scaleY));
            //originalSize = this.Size;
        }
        private void StoreOriginalBoundsRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (!originalControlBounds.ContainsKey(ctrl))
                    originalControlBounds[ctrl] = ctrl.Bounds;

                if (ctrl.Controls.Count > 0)
                    StoreOriginalBoundsRecursive(ctrl);
            }
        }
        private void ResizeControlsRecursive(Control parent)
        {
            if (originalSize.Width == 0 || originalSize.Height == 0) return;

            float xRatio = (float)this.Width / originalSize.Width;
            float yRatio = (float)this.Height / originalSize.Height;

            foreach (Control ctrl in parent.Controls)
            {
                if (originalControlBounds.ContainsKey(ctrl))
                {
                    Rectangle orig = originalControlBounds[ctrl];
                    int newX = (int)(orig.X * xRatio);
                    int newY = (int)(orig.Y * yRatio);
                    int newWidth = (int)(orig.Width * xRatio);
                    int newHeight = (int)(orig.Height * yRatio);
                    ctrl.Bounds = new Rectangle(newX, newY, newWidth, newHeight);
                }
                if (ctrl.Controls.Count > 0)
                {
                    ResizeControlsRecursive(ctrl);
                }
            }
        }

        private void challangeBitton_Click(object sender, EventArgs e)
        {
            this.Hide();
            mini_gra form_mini_gry = new mini_gra();
            form_mini_gry.ShowDialog();
            this.Close();
        }

        private void getPhotoButton_Click(object sender, EventArgs e)
        {
            photoPath.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            photoPath.Title = "Wybierz zdjęcie";
            if (photoPath.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Image img = Image.FromFile(photoPath.FileName);
                    pictureToCheck.Image = img;
                    pictureToCheck.SizeMode = PictureBoxSizeMode.Zoom;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd podczas ładowania zdjęcia");
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(photoPath.FileName))
            {
                MessageBox.Show("Nie podano zdjęcia.");
                return;
            }
            try
            {
                pictureToCheck.Image = Image.FromFile(photoPath.FileName);
                pictureToCheck.SizeMode = PictureBoxSizeMode.Zoom;

                result_from_model = await SendImageToModel(photoPath.FileName);
                if (result_from_model == 0)
                    answerAIorNOT.Text="Model mówi: to nie jest AI";
                else if (result_from_model == 1)
                    answerAIorNOT.Text="Model mówi: to jest AI";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas ładowania zdjęcia\n{ex.Message}");
                return;
            }
        }
        private async Task<int> SendImageToModel(string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/");
                using (var content = new MultipartFormDataContent())
                {
                    var imageContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                    string ext=System.IO.Path.GetExtension(filePath).ToLower();
                    string mime = "image/jpeg";
                    if (ext == ".png") mime = "image/png";
                    else if (ext == ".bmp") mime = "image/bmp";
                    else if (ext == ".gif") mime = "image/gif";
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
                    content.Add(imageContent, "file", System.IO.Path.GetFileName(filePath));
                    
                    HttpResponseMessage response = await client.PostAsync("predict", content);
                    response.EnsureSuccessStatusCode();

                    var responseString = await response.Content.ReadAsStringAsync();
                    using(var doc=JsonDocument.Parse(responseString))
                    {
                        int prediction = doc.RootElement.GetProperty("result").GetInt32();
                        return prediction;
                    }
                }
            }
        }
    }
}
