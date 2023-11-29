using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Final_merejeve
{
    public partial class Form1 : Form
    {
        WebClient webClient;
        string filePathToSave=null;

        public Form1()
        {
            InitializeComponent();
            webClient = new WebClient();
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);
            label1.BackColor = Color.Transparent;
            label2.BackColor = Color.Transparent;
            label4.BackColor = Color.Transparent;
            label3.BackColor = Color.Transparent;
            label5.BackColor = Color.Transparent;
            label6.BackColor = Color.Transparent;
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            string url = txtURL.Text;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            try
            {
                Uri uri = new Uri(url);
                string extension = Path.GetExtension(uri.AbsolutePath);

                if (string.IsNullOrEmpty(extension)) 
                {
                    throw new ArgumentException("Розширення файлу в URL не знайдено");
                }

                saveFileDialog.Filter = $"Files (*{extension})|*{extension}";
                saveFileDialog.FileName = "downloaded_file" + extension;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePathToSave = saveFileDialog.FileName; 

                    listBoxCurrentlyDownloading.Items.Add(filePathToSave);

                    int threadsCount = (int)numericUpDownThreads.Value;

                    var tasks = new List<Task>();

                    for (int i = 0; i < threadsCount; i++)
                    {
                        tasks.Add(DownloadFileAsync(url, filePathToSave));
                    }

                    await Task.WhenAll(tasks);

                    listBoxFiles.Items.Add(filePathToSave);
                    listBoxCurrentlyDownloading.Items.Remove(filePathToSave); 

                    MessageBox.Show("Файл успішно завантажено!");
                }
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"Помилка в URL: {ex.Message}");
                listBoxFailedDownloads.Items.Add(filePathToSave);
                listBoxCurrentlyDownloading.Items.Remove(filePathToSave);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
                if (filePathToSave != null)
                {
                    listBoxFailedDownloads.Items.Add(filePathToSave);
                    listBoxCurrentlyDownloading.Items.Remove(filePathToSave);
                }

            }
            catch (PathTooLongException ex)
            {
                MessageBox.Show($"Довгий шлях файлу: {ex.Message}");
                if (filePathToSave != null)
                {
                    listBoxFailedDownloads.Items.Add(filePathToSave);
                    listBoxCurrentlyDownloading.Items.Remove(filePathToSave);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Виникла помилка: {ex.Message}");
                if (filePathToSave != null)
                {
                    listBoxFailedDownloads.Items.Add(filePathToSave);
                    listBoxCurrentlyDownloading.Items.Remove(filePathToSave);
                }
            }

        }

        private async Task DownloadFileAsync(string url, string filePathToSave)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    using (var fileStream = File.Create(filePathToSave))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    MessageBox.Show($"Не вдалося завантажити файл. Код: {response.StatusCode}");
                }
            }
        }



        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string fileName = listBoxCurrentlyDownloading.Items.Cast<string>().LastOrDefault();

            if (e.Error != null)
            {
                MessageBox.Show("Помилка завантаження: " + e.Error.Message);

                if (fileName != null)
                {
                    listBoxFailedDownloads.Items.Add(fileName);
                }
            }
            else
            {
                MessageBox.Show("Файл успішно завантажено!");

                if (fileName != null)
                {
                    listBoxFiles.Items.Add(fileName);
                }
            }
            if (fileName != null)
            {
                listBoxCurrentlyDownloading.Items.Remove(fileName);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (webClient.IsBusy)
            {
                webClient.CancelAsync();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex != -1)
            {
                string selectedFilePath = listBoxFiles.SelectedItem.ToString();
                if (File.Exists(selectedFilePath))
                {
                    File.Delete(selectedFilePath); 
                    listBoxFiles.Items.Remove(selectedFilePath);
                }
                else
                {
                    MessageBox.Show("Файл не знайдений.");
                }
            }

        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex != -1)
            {
                string selectedFilePath = listBoxFiles.SelectedItem.ToString();
                string directoryPath = Path.GetDirectoryName(selectedFilePath);

                string extension = Path.GetExtension(selectedFilePath); 

                string newFileName = textBoxNewName.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newFileName))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFilePath); 
                    string newFilePath = Path.Combine(directoryPath, newFileName + extension); 

                    if (File.Exists(selectedFilePath))
                    {
                        File.Move(selectedFilePath, newFilePath);
                        listBoxFiles.Items[listBoxFiles.SelectedIndex] = newFilePath;
                        MessageBox.Show($"Файл успішно перейменований в {newFileName}{extension}");
                    }
                    else
                    {
                        MessageBox.Show("Файл не знайдений.");
                    }
                }
            }
        }

        private void btnMoveFile_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex != -1)
            {
                string selectedFilePath = listBoxFiles.SelectedItem.ToString();

                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Виберіть папку для переміщення файлу";

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        string destinationFolderPath = folderDialog.SelectedPath; 

                        try
                        {
                            string fileName = Path.GetFileName(selectedFilePath);
                            string destinationFilePath = Path.Combine(destinationFolderPath, fileName); 

                            File.Move(selectedFilePath, destinationFilePath); 
                            listBoxFiles.Items.Remove(selectedFilePath); 
                            listBoxFiles.Items.Add(destinationFilePath); 

                            MessageBox.Show("Файл успішно переміщено!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Помилка переміщення файлу: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Щоб завантажити файл з інтернету потрібно ввести URL-адресу в поле для пошуку, вибрати кількість потоків (за необхідності), папку, в яку буде завантажено файл та натиснути \"Завантажити\".\n\nЩоб припинити процес завантаження поруч є кнопка \"Скасувати\".\n\nДля того, щоб видалити файл, необхідно натиснути на необхідний шлях до файлу в таблиці \"Вдалі завантаження\" та натиснути кнопку \"Видалити\".\n\nДля того, щоб перейменувати файл, необхідно натиснути на необхідний шлях до файлу в таблиці \"Вдалі завантаження\", написати в полі під кнопкою нову назву файлу (без розширення) та натиснути кнопку \"Перейменувати\".\n\nДля того, щоб перемістити файл, необхідно натиснути на необхідний шлях до файлу в таблиці \"Вдалі завантаження\", натиснути кнопку \"Перемістити\" та вибрати нову папку.", "Довідка");
        }
    }
}
