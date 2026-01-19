using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using ModelessForms.IssuesManager.Handlers;
using ModelessForms.IssuesManager.Models;
using ModelessForms.IssuesManager.Services;
using WinForms = System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace ModelessForms.IssuesManager.Views
{
    public partial class IssueTrackerWindow : Window
    {
        private ExternalEvent _externalEvent;
        private ExternalEventHandler _handler;
        private Application _app;

        private StorageService _storageService;
        private ScreenshotService _screenshotService;
        private SpeechService _speechService;
        private PdfExportService _pdfExportService;

        private Settings _settings;
        private Collection _currentCollection;
        private Issue _selectedIssue;
        private ObservableCollection<BitmapImage> _screenshotImages;

        private Action<string> _inputCallback;
        private bool _isUpdatingUI;

        private WpfComboBox CollectionComboBox;
        private ListBox IssuesListBox;
        private Grid DetailPanel;
        private WpfTextBox DescriptionTextBox;
        private TextBlock IssueIdLabel;
        private ListBox ElementsListBox;
        private ListBox ScreenshotsListBox;
        private Button VoiceButton;
        private Border SettingsOverlay;
        private WpfTextBox BaseFolderTextBox;
        private WpfTextBox AzureKeyTextBox;
        private WpfTextBox AzureRegionTextBox;
        private WpfComboBox MicrophoneComboBox;
        private WpfComboBox LanguageComboBox;
        private Border InputOverlay;
        private TextBlock InputPromptLabel;
        private WpfTextBox InputTextBox;
        private Border CollectionSettingsOverlay;
        private WpfTextBox CollectionProjectNameTextBox;
        private WpfTextBox CollectionAuthorNameTextBox;
        private bool _isNewCollectionFlow;

        public IssueTrackerWindow(ExternalEvent exEvent, ExternalEventHandler handler, Application app)
        {
            _externalEvent = exEvent;
            _handler = handler;
            _app = app;

            _storageService = new StorageService();
            _screenshotService = new ScreenshotService();
            _pdfExportService = new PdfExportService();
            _screenshotImages = new ObservableCollection<BitmapImage>();

            BuildUI();

            ScreenshotsListBox.ItemsSource = _screenshotImages;

            LoadSettings();
            InitializeSpeechService();
            LoadCollections();

            Closing += IssueTrackerWindow_Closing;
        }

        private void BuildUI()
        {
            Title = "Issue Tracker";
            Width = 1000;
            Height = 700;
            MinWidth = 800;
            MinHeight = 500;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            mainGrid.Children.Add(CreateHeader());
            Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 0);

            mainGrid.Children.Add(CreateMainContent());
            Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 1);

            mainGrid.Children.Add(CreateFooter());
            Grid.SetRow(mainGrid.Children[mainGrid.Children.Count - 1], 2);

            SettingsOverlay = CreateSettingsOverlay();
            mainGrid.Children.Add(SettingsOverlay);
            Grid.SetRowSpan(SettingsOverlay, 3);

            InputOverlay = CreateInputOverlay();
            mainGrid.Children.Add(InputOverlay);
            Grid.SetRowSpan(InputOverlay, 3);

            CollectionSettingsOverlay = CreateCollectionSettingsOverlay();
            mainGrid.Children.Add(CollectionSettingsOverlay);
            Grid.SetRowSpan(CollectionSettingsOverlay, 3);

            Content = mainGrid;
        }

        private Border CreateHeader()
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Padding = new Thickness(10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock { Text = "Collection:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            CollectionComboBox = new WpfComboBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                ItemContainerStyle = CreateDarkComboBoxItemStyle()
            };
            CollectionComboBox.SelectionChanged += CollectionComboBox_SelectionChanged;
            Grid.SetColumn(CollectionComboBox, 1);
            grid.Children.Add(CollectionComboBox);

            var newCollBtn = CreateButton("New", "#333337");
            newCollBtn.Click += NewCollectionButton_Click;
            newCollBtn.Margin = new Thickness(10, 0, 5, 0);
            Grid.SetColumn(newCollBtn, 2);
            grid.Children.Add(newCollBtn);

            var editCollBtn = CreateButton("Edit", "#333337");
            editCollBtn.Click += EditCollectionButton_Click;
            editCollBtn.Margin = new Thickness(0, 0, 5, 0);
            Grid.SetColumn(editCollBtn, 3);
            grid.Children.Add(editCollBtn);

            var delCollBtn = CreateButton("Delete", "#6E1E1E");
            delCollBtn.Click += DeleteCollectionButton_Click;
            Grid.SetColumn(delCollBtn, 4);
            grid.Children.Add(delCollBtn);

            var settingsBtn = CreateButton("Settings", "#333337");
            settingsBtn.Click += SettingsButton_Click;
            Grid.SetColumn(settingsBtn, 6);
            grid.Children.Add(settingsBtn);

            border.Child = grid;
            return border;
        }

        private Grid CreateMainContent()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280), MinWidth = 200 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerBorder = new Border { Padding = new Thickness(10), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")), BorderThickness = new Thickness(0, 0, 0, 1) };
            headerBorder.Child = new TextBlock { Text = "Issues", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(headerBorder, 0);
            leftGrid.Children.Add(headerBorder);

            IssuesListBox = new ListBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ItemContainerStyle = CreateDarkListBoxItemStyle()
            };
            IssuesListBox.SelectionChanged += IssuesListBox_SelectionChanged;
            Grid.SetRow(IssuesListBox, 1);
            leftGrid.Children.Add(IssuesListBox);

            leftPanel.Child = leftGrid;
            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            var splitter = new GridSplitter { Width = 4, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")), HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(splitter, 1);
            grid.Children.Add(splitter);

            var rightPanel = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                Padding = new Thickness(15)
            };

            DetailPanel = CreateDetailPanel();
            DetailPanel.Visibility = Visibility.Collapsed;
            rightPanel.Child = DetailPanel;

            Grid.SetColumn(rightPanel, 2);
            grid.Children.Add(rightPanel);

            return grid;
        }

        private Grid CreateDetailPanel()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var descPanel = new StackPanel();
            var descHeaderGrid = new Grid();
            descHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            descHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            descHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            descHeaderGrid.Children.Add(new TextBlock { Text = "Description:", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });

            VoiceButton = CreateButton("\u25CF", "#8B0000");
            VoiceButton.Click += VoiceButton_Click;
            VoiceButton.Margin = new Thickness(0, 0, 5, 8);
            VoiceButton.Width = 32;
            VoiceButton.FontSize = 16;
            VoiceButton.ToolTip = "Start recording";
            Grid.SetColumn(VoiceButton, 1);
            descHeaderGrid.Children.Add(VoiceButton);

            var clearBtn = CreateButton("Clear", "#333337");
            clearBtn.Click += ClearDescButton_Click;
            clearBtn.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumn(clearBtn, 2);
            descHeaderGrid.Children.Add(clearBtn);

            descPanel.Children.Add(descHeaderGrid);

            DescriptionTextBox = new WpfTextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                CaretBrush = Brushes.White
            };
            DescriptionTextBox.TextChanged += DescriptionTextBox_TextChanged;
            descPanel.Children.Add(DescriptionTextBox);

            Grid.SetRow(descPanel, 0);
            grid.Children.Add(descPanel);

            var screenPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };
            var screenHeaderGrid = new Grid();
            screenHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            screenHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            screenHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            screenHeaderGrid.Children.Add(new TextBlock { Text = "Screenshots:", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });

            var addScreenBtn = CreateButton("Add", "#333337");
            addScreenBtn.Click += AddScreenshotButton_Click;
            addScreenBtn.Margin = new Thickness(0, 0, 5, 8);
            Grid.SetColumn(addScreenBtn, 1);
            screenHeaderGrid.Children.Add(addScreenBtn);

            var removeScreenBtn = CreateButton("Remove", "#6E1E1E");
            removeScreenBtn.Click += RemoveScreenshotButton_Click;
            removeScreenBtn.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumn(removeScreenBtn, 2);
            screenHeaderGrid.Children.Add(removeScreenBtn);

            screenPanel.Children.Add(screenHeaderGrid);

            ScreenshotsListBox = new ListBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Foreground = Brushes.White,
                Height = 80,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                ItemContainerStyle = CreateDarkListBoxItemStyle()
            };
            var wrapPanel = new ItemsPanelTemplate();
            var wrapFactory = new FrameworkElementFactory(typeof(WrapPanel));
            wrapFactory.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
            wrapPanel.VisualTree = wrapFactory;
            ScreenshotsListBox.ItemsPanel = wrapPanel;
            screenPanel.Children.Add(ScreenshotsListBox);

            Grid.SetRow(screenPanel, 1);
            grid.Children.Add(screenPanel);

            var elemPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };
            var elemHeaderGrid = new Grid();
            elemHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            elemHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            elemHeaderGrid.Children.Add(new TextBlock { Text = "Related Elements:", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });

            var selectElemBtn = CreateButton("Select in Revit", "#333337");
            selectElemBtn.Click += SelectElementsButton_Click;
            selectElemBtn.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumn(selectElemBtn, 1);
            elemHeaderGrid.Children.Add(selectElemBtn);

            elemPanel.Children.Add(elemHeaderGrid);

            ElementsListBox = new ListBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0")),
                Height = 100,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                FontFamily = new FontFamily("Consolas"),
                ItemContainerStyle = CreateDarkListBoxItemStyle()
            };
            elemPanel.Children.Add(ElementsListBox);

            Grid.SetRow(elemPanel, 2);
            grid.Children.Add(elemPanel);

            IssueIdLabel = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0")),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(IssueIdLabel, 3);
            grid.Children.Add(IssueIdLabel);

            return grid;
        }

        private Border CreateFooter()
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Padding = new Thickness(10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var newIssueBtn = CreateButton("New Issue", "#2D5A2D");
            newIssueBtn.Click += NewIssueButton_Click;
            newIssueBtn.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(newIssueBtn, 0);
            grid.Children.Add(newIssueBtn);

            var delIssueBtn = CreateButton("Delete Issue", "#6E1E1E");
            delIssueBtn.Click += DeleteIssueButton_Click;
            delIssueBtn.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(delIssueBtn, 1);
            grid.Children.Add(delIssueBtn);

            var saveBtn = CreateButton("Save", "#333337");
            saveBtn.Click += SaveButton_Click;
            Grid.SetColumn(saveBtn, 2);
            grid.Children.Add(saveBtn);

            var publishBtn = CreateButton("Publish to PDF", "#5B2C6F");
            publishBtn.Click += PublishPdfButton_Click;
            Grid.SetColumn(publishBtn, 4);
            grid.Children.Add(publishBtn);

            border.Child = grid;
            return border;
        }

        private Border CreateSettingsOverlay()
        {
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0, 0, 0)),
                Visibility = Visibility.Collapsed
            };

            var dialog = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Width = 450,
                Height = 350,
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock { Text = "Settings", Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 15) };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            var content = new StackPanel();

            content.Children.Add(new TextBlock { Text = "Storage Folder:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) });
            var folderGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            BaseFolderTextBox = new WpfTextBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")), Foreground = Brushes.White, IsReadOnly = true };
            folderGrid.Children.Add(BaseFolderTextBox);
            var browseBtn = CreateButton("Browse", "#333337");
            browseBtn.Click += BrowseFolderButton_Click;
            browseBtn.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(browseBtn, 1);
            folderGrid.Children.Add(browseBtn);
            content.Children.Add(folderGrid);

            content.Children.Add(new TextBlock { Text = "Azure Speech Key:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) });
            AzureKeyTextBox = new WpfTextBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")), Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 15) };
            content.Children.Add(AzureKeyTextBox);

            content.Children.Add(new TextBlock { Text = "Azure Speech Region:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) });
            AzureRegionTextBox = new WpfTextBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")), Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            content.Children.Add(AzureRegionTextBox);
            content.Children.Add(new TextBlock { Text = "(e.g., eastus, westeurope, etc.)", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0")), Margin = new Thickness(0, 0, 0, 15) });

            content.Children.Add(new TextBlock { Text = "Microphone:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) });
            MicrophoneComboBox = new WpfComboBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                ItemContainerStyle = CreateDarkComboBoxItemStyle(),
                Margin = new Thickness(0, 0, 0, 5)
            };
            MicrophoneComboBox.Items.Add("(Default)");
            foreach (var mic in SpeechService.GetAvailableMicrophones())
                MicrophoneComboBox.Items.Add(mic.FriendlyName);
            MicrophoneComboBox.SelectedIndex = 0;
            content.Children.Add(MicrophoneComboBox);
            content.Children.Add(new TextBlock { Text = "Leave as Default or select your USB microphone", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0")), Margin = new Thickness(0, 0, 0, 15) });

            content.Children.Add(new TextBlock { Text = "Speech Language:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) });
            LanguageComboBox = new WpfComboBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                ItemContainerStyle = CreateDarkComboBoxItemStyle(),
                Margin = new Thickness(0, 0, 0, 5)
            };
            foreach (var lang in SpeechService.GetSupportedLanguages())
                LanguageComboBox.Items.Add(lang.Name);
            LanguageComboBox.SelectedIndex = 0;
            content.Children.Add(LanguageComboBox);

            Grid.SetRow(content, 1);
            grid.Children.Add(content);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveSettingsBtn = CreateButton("Save", "#2D5A2D");
            saveSettingsBtn.Click += SaveSettingsButton_Click;
            saveSettingsBtn.Margin = new Thickness(0, 0, 10, 0);
            buttons.Children.Add(saveSettingsBtn);
            var cancelSettingsBtn = CreateButton("Cancel", "#333337");
            cancelSettingsBtn.Click += CancelSettingsButton_Click;
            buttons.Children.Add(cancelSettingsBtn);
            Grid.SetRow(buttons, 2);
            grid.Children.Add(buttons);

            dialog.Child = grid;
            overlay.Child = dialog;
            return overlay;
        }

        private Border CreateInputOverlay()
        {
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0, 0, 0)),
                Visibility = Visibility.Collapsed
            };

            var dialog = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Width = 350,
                Height = 150,
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            InputPromptLabel = new TextBlock { Text = "Enter collection name:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(InputPromptLabel, 0);
            grid.Children.Add(InputPromptLabel);

            InputTextBox = new WpfTextBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")), Foreground = Brushes.White };
            Grid.SetRow(InputTextBox, 1);
            grid.Children.Add(InputTextBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            var okBtn = CreateButton("OK", "#2D5A2D");
            okBtn.Click += InputOkButton_Click;
            okBtn.Margin = new Thickness(0, 0, 10, 0);
            buttons.Children.Add(okBtn);
            var cancelBtn = CreateButton("Cancel", "#333337");
            cancelBtn.Click += InputCancelButton_Click;
            buttons.Children.Add(cancelBtn);
            Grid.SetRow(buttons, 3);
            grid.Children.Add(buttons);

            dialog.Child = grid;
            overlay.Child = dialog;
            return overlay;
        }

        private Border CreateCollectionSettingsOverlay()
        {
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0, 0, 0)),
                Visibility = Visibility.Collapsed
            };

            var dialog = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526")),
                Width = 400,
                Height = 250,
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleLabel = new TextBlock
            {
                Text = "Collection Settings",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(titleLabel, 0);
            grid.Children.Add(titleLabel);

            var projectLabel = new TextBlock { Text = "Project Name:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(projectLabel, 1);
            grid.Children.Add(projectLabel);

            CollectionProjectNameTextBox = new WpfTextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(CollectionProjectNameTextBox, 2);
            grid.Children.Add(CollectionProjectNameTextBox);

            var authorLabel = new TextBlock { Text = "Author Name:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(authorLabel, 3);
            grid.Children.Add(authorLabel);

            CollectionAuthorNameTextBox = new WpfTextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(CollectionAuthorNameTextBox, 4);
            grid.Children.Add(CollectionAuthorNameTextBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            var saveBtn = CreateButton("Save", "#2D5A2D");
            saveBtn.Click += CollectionSettingsOkButton_Click;
            saveBtn.Margin = new Thickness(0, 0, 10, 0);
            buttons.Children.Add(saveBtn);
            var cancelBtn = CreateButton("Cancel", "#333337");
            cancelBtn.Click += CollectionSettingsCancelButton_Click;
            buttons.Children.Add(cancelBtn);
            Grid.SetRow(buttons, 6);
            grid.Children.Add(buttons);

            dialog.Child = grid;
            overlay.Child = dialog;
            return overlay;
        }

        private Button CreateButton(string content, string backgroundColor)
        {
            var btn = new Button
            {
                Content = content,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            return btn;
        }

        private Style CreateDarkListBoxItemStyle()
        {
            var style = new Style(typeof(ListBoxItem));
            style.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(8, 6, 8, 6)));
            style.Setters.Add(new Setter(ListBoxItem.BorderThicknessProperty, new Thickness(0)));

            var mouseOverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E42"))));
            style.Triggers.Add(mouseOverTrigger);

            var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"))));
            selectedTrigger.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
            style.Triggers.Add(selectedTrigger);

            return style;
        }

        private Style CreateDarkComboBoxItemStyle()
        {
            var style = new Style(typeof(ComboBoxItem));
            style.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337"))));
            style.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(8, 4, 8, 4)));

            var mouseOverTrigger = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"))));
            style.Triggers.Add(mouseOverTrigger);

            var selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"))));
            style.Triggers.Add(selectedTrigger);

            return style;
        }

        private void IssueTrackerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCollection();
            _speechService?.Dispose();
        }

        private void LoadSettings()
        {
            _settings = _storageService.LoadSettings();
            if (string.IsNullOrEmpty(_settings.BaseFolder) || !Directory.Exists(_settings.BaseFolder))
            {
                ShowSettingsOverlay();
            }
        }

        private void InitializeSpeechService()
        {
            _speechService?.Dispose();
            _speechService = new SpeechService(_settings.AzureSpeechKey, _settings.AzureSpeechRegion, _settings.MicrophoneName, _settings.SpeechLanguage);
            _speechService.OnFinalResult += OnSpeechFinalResult;
            _speechService.OnError += OnSpeechError;
        }

        private void OnSpeechFinalResult(string text)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    if (!string.IsNullOrEmpty(DescriptionTextBox.Text))
                        DescriptionTextBox.Text += Environment.NewLine;
                    DescriptionTextBox.Text += text;
                }
            }));
        }

        private void OnSpeechError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(error, "Speech Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }));
        }

        private void LoadCollections()
        {
            if (string.IsNullOrEmpty(_settings.BaseFolder)) return;

            var collections = _storageService.GetCollectionNames(_settings.BaseFolder);
            if (collections.Count == 0)
            {
                var defaultCollection = new Collection("Default");
                _storageService.SaveCollection(_settings.BaseFolder, defaultCollection);
                collections = new List<string> { "Default" };
                _settings.DefaultCollection = "Default";
                _storageService.SaveSettings(_settings);
            }

            CollectionComboBox.ItemsSource = collections;
            var selectedIndex = collections.IndexOf(_settings.DefaultCollection);
            CollectionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private void LoadCollection(string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(_settings.BaseFolder)) return;

            SaveCollection();
            _currentCollection = _storageService.LoadCollection(_settings.BaseFolder, name);
            IssuesListBox.ItemsSource = _currentCollection.Issues;
            IssuesListBox.DisplayMemberPath = "GetDisplayTitle";

            if (_currentCollection.Issues.Count > 0)
                IssuesListBox.SelectedIndex = 0;
            else
                ClearDetailPanel();

            _settings.DefaultCollection = name;
            _storageService.SaveSettings(_settings);
        }

        private void ClearDetailPanel()
        {
            _selectedIssue = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            _screenshotImages.Clear();
        }

        private void LoadIssue(Issue issue)
        {
            if (issue == null) { ClearDetailPanel(); return; }

            _isUpdatingUI = true;
            _selectedIssue = issue;
            DetailPanel.Visibility = Visibility.Visible;

            DescriptionTextBox.Text = issue.Description ?? string.Empty;
            IssueIdLabel.Text = $"ID: {issue.Id}";
            ElementsListBox.ItemsSource = issue.ElementGuids;

            _screenshotImages.Clear();
            if (issue.Screenshots != null)
            {
                var imagesFolder = _storageService.GetImagesFolder(_settings.BaseFolder, _currentCollection.Name);
                foreach (var screenshot in issue.Screenshots)
                {
                    var path = _screenshotService.GetScreenshotPath(imagesFolder, screenshot);
                    if (File.Exists(path))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(path);
                            bitmap.EndInit();
                            _screenshotImages.Add(bitmap);
                        }
                        catch { }
                    }
                }
            }
            _isUpdatingUI = false;
        }

        private void SaveCurrentIssue()
        {
            if (_selectedIssue == null || _currentCollection == null) return;
            _selectedIssue.Description = DescriptionTextBox.Text;
            _selectedIssue.Modified = DateTime.Now;
        }

        private void SaveCollection()
        {
            if (_currentCollection == null || string.IsNullOrEmpty(_settings.BaseFolder)) return;
            SaveCurrentIssue();
            _storageService.SaveCollection(_settings.BaseFolder, _currentCollection);
        }

        private void CollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CollectionComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected)) LoadCollection(selected);
        }

        private void IssuesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI) return;
            SaveCollection();
            var issue = IssuesListBox.SelectedItem as Issue;
            LoadIssue(issue);
        }

        private void NewCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInputDialog("Enter collection name:", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                name = SanitizeFileName(name);
                var newCollection = new Collection(name);
                _storageService.SaveCollection(_settings.BaseFolder, newCollection);
                LoadCollections();
                CollectionComboBox.SelectedItem = name;

                // Show collection settings overlay for new collection
                _isNewCollectionFlow = true;
                ShowCollectionSettingsOverlay();
            });
        }

        private void EditCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCollection == null) return;
            _isNewCollectionFlow = false;
            ShowCollectionSettingsOverlay();
        }

        private void ShowCollectionSettingsOverlay()
        {
            CollectionProjectNameTextBox.Text = _currentCollection?.ProjectName ?? string.Empty;
            CollectionAuthorNameTextBox.Text = _currentCollection?.AuthorName ?? string.Empty;
            CollectionSettingsOverlay.Visibility = Visibility.Visible;
            CollectionProjectNameTextBox.Focus();
        }

        private void CollectionSettingsOkButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionSettingsOverlay.Visibility = Visibility.Collapsed;
            if (_currentCollection != null)
            {
                _currentCollection.ProjectName = CollectionProjectNameTextBox.Text;
                _currentCollection.AuthorName = CollectionAuthorNameTextBox.Text;
                SaveCollection();
            }
        }

        private void CollectionSettingsCancelButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionSettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void DeleteCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = CollectionComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selected)) return;

            var result = MessageBox.Show($"Delete collection '{selected}' and all its issues?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _storageService.DeleteCollection(_settings.BaseFolder, selected);
                _currentCollection = null;
                ClearDetailPanel();
                LoadCollections();
            }
        }

        private void NewIssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCollection == null) return;
            SaveCollection();

            var handler = new GetSelectionHandler();
            handler.Callback = OnSelectionReceived;
            _app.asyncCommand = handler;
            _externalEvent.Raise();
        }

        private void OnSelectionReceived(List<string> guids)
        {
            Dispatcher.Invoke(() =>
            {
                var newIssue = new Issue();
                newIssue.ElementGuids = guids ?? new List<string>();
                _currentCollection.Issues.Insert(0, newIssue);
                IssuesListBox.Items.Refresh();
                IssuesListBox.SelectedIndex = 0;
                SaveCollection();
            });
        }

        private void DeleteIssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIssue == null || _currentCollection == null) return;

            var result = MessageBox.Show("Delete this issue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var imagesFolder = _storageService.GetImagesFolder(_settings.BaseFolder, _currentCollection.Name);
                foreach (var screenshot in _selectedIssue.Screenshots)
                    _screenshotService.DeleteScreenshot(imagesFolder, screenshot);

                _currentCollection.Issues.Remove(_selectedIssue);
                IssuesListBox.Items.Refresh();
                ClearDetailPanel();

                if (_currentCollection.Issues.Count > 0)
                    IssuesListBox.SelectedIndex = 0;

                SaveCollection();
            }
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _selectedIssue == null) return;
            _selectedIssue.Description = DescriptionTextBox.Text;
        }

        private async void VoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_speechService == null || !_speechService.IsConfigured)
            {
                MessageBox.Show("Azure Speech Service is not configured. Please set Azure Speech Key and Region in Settings.", "Voice Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_speechService.IsRecording)
                {
                    await _speechService.StartRecordingAsync();
                    VoiceButton.Content = "\u25A0";
                    VoiceButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
                    VoiceButton.ToolTip = "Stop recording";
                }
                else
                {
                    VoiceButton.IsEnabled = false;
                    VoiceButton.Content = "...";
                    await _speechService.StopRecordingAsync();
                    VoiceButton.Content = "\u25CF";
                    VoiceButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000"));
                    VoiceButton.ToolTip = "Start recording";
                    VoiceButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Voice Recognition Error", MessageBoxButton.OK, MessageBoxImage.Error);
                VoiceButton.Content = "\u25CF";
                VoiceButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000"));
                VoiceButton.ToolTip = "Start recording";
                VoiceButton.IsEnabled = true;
            }
        }

        private void ClearDescButton_Click(object sender, RoutedEventArgs e) => DescriptionTextBox.Text = string.Empty;

        private void AddScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIssue == null || _currentCollection == null) return;
            Hide();
            var overlay = new ScreenshotOverlay();
            overlay.ScreenshotCaptured += OnScreenshotCaptured;
            overlay.Show();
        }

        private void OnScreenshotCaptured(object sender, ScreenshotCapturedEventArgs e)
        {
            Show();
            if (e.ImageData == null || e.ImageData.Length == 0) return;

            var imagesFolder = _storageService.GetImagesFolder(_settings.BaseFolder, _currentCollection.Name);
            var index = _selectedIssue.Screenshots.Count + 1;
            var fileName = _screenshotService.SaveScreenshot(e.ImageData, imagesFolder, _selectedIssue.Id, index);

            if (!string.IsNullOrEmpty(fileName))
            {
                _selectedIssue.Screenshots.Add(fileName);
                var path = _screenshotService.GetScreenshotPath(imagesFolder, fileName);
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path);
                    bitmap.EndInit();
                    _screenshotImages.Add(bitmap);
                }
                catch { }
                SaveCollection();
            }
        }

        private void RemoveScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIssue == null || ScreenshotsListBox.SelectedIndex < 0) return;
            var index = ScreenshotsListBox.SelectedIndex;
            if (index >= 0 && index < _selectedIssue.Screenshots.Count)
            {
                var fileName = _selectedIssue.Screenshots[index];
                var imagesFolder = _storageService.GetImagesFolder(_settings.BaseFolder, _currentCollection.Name);
                _screenshotService.DeleteScreenshot(imagesFolder, fileName);
                _selectedIssue.Screenshots.RemoveAt(index);
                _screenshotImages.RemoveAt(index);
                SaveCollection();
            }
        }

        private void SelectElementsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIssue == null || _selectedIssue.ElementGuids == null || _selectedIssue.ElementGuids.Count == 0) return;
            var handler = new SelectElementsHandler(_selectedIssue.ElementGuids);
            _app.asyncCommand = handler;
            _externalEvent.Raise();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCollection();
            MessageBox.Show("Collection saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PublishPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCollection == null || _currentCollection.Issues.Count == 0)
            {
                MessageBox.Show("No issues to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_pdfExportService.IsSupported)
            {
                MessageBox.Show("PDF export requires Revit 2025 or later.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveCollection();
            var dialog = new WinForms.SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"{_currentCollection.Name}_{DateTime.Now:yyyyMMdd}.pdf" };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                try
                {
                    _pdfExportService.ExportToPdf(_currentCollection, dialog.FileName, _settings.BaseFolder);
                    MessageBox.Show($"PDF exported to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettingsOverlay();

        private void ShowSettingsOverlay()
        {
            BaseFolderTextBox.Text = _settings.BaseFolder ?? string.Empty;
            AzureKeyTextBox.Text = _settings.AzureSpeechKey ?? string.Empty;
            AzureRegionTextBox.Text = _settings.AzureSpeechRegion ?? string.Empty;

            var micIndex = 0;
            if (!string.IsNullOrEmpty(_settings.MicrophoneName))
            {
                for (int i = 0; i < MicrophoneComboBox.Items.Count; i++)
                {
                    if (MicrophoneComboBox.Items[i].ToString() == _settings.MicrophoneName)
                    {
                        micIndex = i;
                        break;
                    }
                }
            }
            MicrophoneComboBox.SelectedIndex = micIndex;

            var langIndex = 0;
            var languages = SpeechService.GetSupportedLanguages();
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i].Code == _settings.SpeechLanguage)
                {
                    langIndex = i;
                    break;
                }
            }
            LanguageComboBox.SelectedIndex = langIndex;

            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog { Description = "Select storage folder for issue collections", SelectedPath = _settings.BaseFolder };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                BaseFolderTextBox.Text = dialog.SelectedPath;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BaseFolderTextBox.Text))
            {
                MessageBox.Show("Please select a storage folder.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _settings.BaseFolder = BaseFolderTextBox.Text;
            _settings.AzureSpeechKey = AzureKeyTextBox.Text;
            _settings.AzureSpeechRegion = AzureRegionTextBox.Text;

            var selectedMic = MicrophoneComboBox.SelectedItem?.ToString();
            _settings.MicrophoneName = (selectedMic == "(Default)") ? string.Empty : selectedMic;

            var languages = SpeechService.GetSupportedLanguages();
            var langIndex = LanguageComboBox.SelectedIndex;
            _settings.SpeechLanguage = (langIndex >= 0 && langIndex < languages.Count) ? languages[langIndex].Code : "da-DK";

            _storageService.SaveSettings(_settings);
            InitializeSpeechService();
            SettingsOverlay.Visibility = Visibility.Collapsed;
            LoadCollections();
        }

        private void CancelSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_settings.BaseFolder)) { Close(); return; }
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ShowInputDialog(string prompt, Action<string> callback)
        {
            _inputCallback = callback;
            InputPromptLabel.Text = prompt;
            InputTextBox.Text = string.Empty;
            InputOverlay.Visibility = Visibility.Visible;
            InputTextBox.Focus();
        }

        private void InputOkButton_Click(object sender, RoutedEventArgs e)
        {
            InputOverlay.Visibility = Visibility.Collapsed;
            _inputCallback?.Invoke(InputTextBox.Text);
        }

        private void InputCancelButton_Click(object sender, RoutedEventArgs e) => InputOverlay.Visibility = Visibility.Collapsed;

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
