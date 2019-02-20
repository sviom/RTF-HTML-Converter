using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using RichTextBoxResearch.Converter;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace RichTextBoxResearch
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
            => RichEditBoxTest.Document.Selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Toggle;

        private async void SaveButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we 
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (var randAccStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    RichEditBoxTest.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }

                // Let Windows know that we're finished changing the file so the 
                // other app can update the remote version of the file.
                var status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    var errorBox = new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }

        }

        /// <summary>
        /// RTF형식을 Html 형식으로
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            RichEditBoxTest.Document.GetText(Windows.UI.Text.TextGetOptions.FormatRtf, out string fvff);
            string htmlcode = await RtfToHtmlConverter.ParseRtfText(fvff);

            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            // WebView navigation은 LoaclFolder 바로 밑에서는 동작하지 않아 폴더 새로 만들어줌
            var testFolder = await storageFolder.CreateFolderAsync("TestFolder", CreationCollisionOption.ReplaceExisting);
            var testHtmlFile = await testFolder.CreateFileAsync("rtftohtmltestpage.html", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(testHtmlFile, htmlcode);

            RtfToHtmlViewer.Navigate(new Uri("ms-appdata:///local/TestFolder/rtftohtmltestpage.html"));

            // Html code 출력
            HtmlCodeViewer.Text = htmlcode;
        }

        /// <summary>
        /// Html 텍스트를 RTF 또는 RichEditBox에 직접 넣기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToRtfButton_Click(object sender, RoutedEventArgs e)
        {
            // RTF 형식 데이터 얻어오기
            byte[] bytes;
            using (var memory = new InMemoryRandomAccessStream())
            {
                RichEditBoxTest.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, memory);
                var streamToSave = memory.AsStream();
                var dataReader = new DataReader(streamToSave.AsInputStream());
                bytes = new byte[streamToSave.Length];

                await dataReader.LoadAsync((uint)streamToSave.Length);

                dataReader.ReadBytes(bytes);
            }

            string result = System.Text.Encoding.UTF8.GetString(bytes);
            RawTextBlock.Text = result;

            // Clipboard에 복사
            // var data = new DataPackage();
            // data.SetText(result);
            // Clipboard.SetContent(data);
        }

        /// <summary>
        /// 다른페이지로 이동
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveToHtmltoRTFpage_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HtmlToRtf));
        }
    }
}
