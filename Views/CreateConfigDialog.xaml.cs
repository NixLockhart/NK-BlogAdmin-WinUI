using Blog_Manager.Services.Api;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Blog_Manager.Views
{
    public sealed partial class CreateConfigDialog : ContentDialog
    {
        public ConfigCreateRequest? Result { get; private set; }

        public CreateConfigDialog()
        {
            this.InitializeComponent();
            this.XamlRoot = (Application.Current as App)?.Window?.Content?.XamlRoot;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(ConfigKeyTextBox.Text))
            {
                args.Cancel = true;
                ErrorInfoBar.Message = "配置键不能为空";
                ErrorInfoBar.IsOpen = true;
                return;
            }

            if (ConfigTypeComboBox.SelectedItem == null)
            {
                args.Cancel = true;
                ErrorInfoBar.Message = "请选择配置类型";
                ErrorInfoBar.IsOpen = true;
                return;
            }

            // 创建请求对象
            Result = new ConfigCreateRequest
            {
                ConfigKey = ConfigKeyTextBox.Text.Trim(),
                ConfigValue = ConfigValueTextBox.Text,
                ConfigType = (ConfigTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "string",
                Description = DescriptionTextBox.Text,
                IsPublic = IsPublicCheckBox.IsChecked ?? false
            };
        }
    }
}
