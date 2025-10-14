using USJRLedger.Services;

namespace USJRLedger.Views.Common
{
    public partial class ReceiptViewerPage : ContentPage
    {
        private readonly byte[] _receiptData;

        public ReceiptViewerPage(string title, byte[] receiptData)
        {
            InitializeComponent();
            Title = $"Receipt: {title}";
            _receiptData = receiptData;

            LoadReceiptImage();
        }

        private void LoadReceiptImage()
        {
            if (_receiptData != null)
            {
                ReceiptImage.Source = ImageSource.FromStream(() => new MemoryStream(_receiptData));
            }
        }
    }
}