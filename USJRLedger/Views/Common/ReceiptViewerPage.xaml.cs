using System;
using System.IO;

namespace USJRLedger.Views.Common
{
    public partial class ReceiptViewerPage : ContentPage
    {
        private readonly byte[] _receiptData;
        private const string PlaceholderImage = "no_receipt.png";

        public ReceiptViewerPage(string title, byte[] receiptData)
        {
            InitializeComponent();
            Title = $"Receipt: {title}";
            TitleLabel.Text = title;
            _receiptData = receiptData;

            LoadReceiptImage();
        }

        private void LoadReceiptImage()
        {
            try
            {
                if (_receiptData != null && _receiptData.Length > 0)
                {
                    ReceiptImage.Source = ImageSource.FromStream(() => new MemoryStream(_receiptData));
                    MessageLabel.IsVisible = false;
                }
                else
                {
                    // Show placeholder
                    ReceiptImage.Source = PlaceholderImage;
                    MessageLabel.Text = "No receipt available for this transaction.";
                    MessageLabel.IsVisible = true;
                }
            }
            catch
            {
                // In case of any loading issue, fallback to placeholder
                ReceiptImage.Source = PlaceholderImage;
                MessageLabel.Text = "Failed to load receipt. Showing placeholder instead.";
                MessageLabel.IsVisible = true;
            }
        }
    }
}
