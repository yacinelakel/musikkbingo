using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;


namespace MusicBingoGenerator.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Reading command-line arguments
            var inputFile = args.Length > 0 ? args[0] :
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "words.txt");

            var numFiles = args.Length > 1 ?  int.Parse(args[1]) : 10;
            var outputFile = args.Length > 2 ? args[2] : "bingo.pdf";

            var card = new BingoCard();

            List<string> playlistSongs = null;
            try
            {
                playlistSongs = File.ReadLines(inputFile).Distinct().ToList();
                
            }
            catch (FileNotFoundException)
            {
                System.Console.Error.WriteLine($"File {inputFile} doesn't exist. Stopping.");
                Environment.Exit(5);
            }

            playlistSongs.ForEach(artistSong =>
            {
                var parsed = artistSong.Split("-", StringSplitOptions.RemoveEmptyEntries);
                card.AddWord(parsed[1], parsed[0]);
            });

            for (int i = 0; i < numFiles; i++)
            {
                card.NewCard("Hemit Musikkbingo", i+1).CreatePdf().SavePdf($"bingo_{i+1}.pdf");
            }
        }
    }


    public class BingoFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string name, bool bold, bool italic)
        {
            var info = new FontResolverInfo(DefaultFontFiles[Convert.ToInt32(bold) + 2 * Convert.ToInt32(italic)]);
            return info;
        }

        /// <summary>Returns .ttf file for the requested font.</summary>
        public byte[] GetFont(string name)
        {
            using (var ms = new MemoryStream())
            {
                var assembly = Assembly.GetEntryAssembly();
                var stream = assembly.GetManifestResourceStream($"MusicBingoGenerator.Console.Fonts.{name}.ttf");
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public string DefaultFontName => "OpenSans-Regular";

        public static readonly string[] DefaultFontFiles = new string[]
        {
            "OpenSans-Regular",
            "OpenSans-Bold",
            "OpenSans-Italic",
            "OpenSans-BoldItalic",
        };
    }

    public class BingoCard
    {
        static BingoCard()
        {
            GlobalFontSettings.FontResolver = new BingoFontResolver();
        }

        public const string FREE_WORD = "FREE";

        public BingoCard()
        {
            Rand = new Random();
            ArtistSong = new List<(string, string)>();
           
            Font = new XFont("OpenSans", FontRegularSize, XFontStyle.Regular);
            ItalicFont = new XFont("OpenSans", FontRegularSize, XFontStyle.Italic);
            TitleFont = new XFont("OpenSans", FontTitleSize, XFontStyle.Bold);
            SubTitleFont = new XFont("OpenSans", 14, XFontStyle.Bold);
        }

        public XFont SubTitleFont { get; set; }

        public BingoCard NewCard(string title, int cardNumber)
        {
            InternalDocument = new PdfDocument();
            InternalDocument.Info.Title = title;
            InternalDocument.Info.Author = "Yacine";
            InternalDocument.Info.Creator = "Yacine";
            Page = InternalDocument.AddPage();
            Page.TrimMargins.All = XUnit.FromCentimeter(2);
            CardNumber = cardNumber;
            Gfx = XGraphics.FromPdfPage(Page);
            XText = new XTextFormatter(Gfx);

            return this;
        }

        public int CardNumber { get; set; }

        public XFont ItalicFont { get; set; }

        private XTextFormatter XText { get; set; }

        public BingoCard CreatePdf()
        {
            var shuffledWords = ArtistSong.OrderBy(x => Rand.Next() ).ToList();

            Gfx.DrawString
            (
                text: InternalDocument.Info.Title,
                font: TitleFont,
                brush: XBrushes.Black,
                layoutRectangle: new XRect(0, 0, Page.Width, (Page.Height.Value / 20)),
                format: XStringFormats.Center
            );

            Gfx.DrawString
            (
                text: $"#{CardNumber}",
                font: TitleFont,
                brush: XBrushes.Black,
                layoutRectangle: new XRect(0, (Page.Height.Value / 20), Page.Width, (Page.Height.Value / 20)),
                format: XStringFormats.Center
            );

            //Which word (from the shuffled list) should we use now?
            var index = 0;

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    //Don't print anything in the middle cell
                    if (i == 2 && j == 2)
                    {
                        Gfx.DrawString
                        (
                            text: FREE_WORD,
                            font: TitleFont,
                            brush: XBrushes.Black,
                            layoutRectangle: new XRect
                            (
                                Page.Width.Value / 5 * i + 5,
                                Page.Height.Value / 8 * (2 + j),
                                Page.Width.Value / 5 - 5,
                                Page.Height.Value / 8
                            ),
                            format: XStringFormats.Center
                        );
                        continue;
                    };


                    var songParts = shuffledWords[index].Item2.Split(@"\n", StringSplitOptions.RemoveEmptyEntries);
                    var artistParts = shuffledWords[index].Item1.Split(@"\n",StringSplitOptions.RemoveEmptyEntries);
                    index++;

                    for (int songPartIndex = 0; songPartIndex < songParts.Length; songPartIndex++)
                    {
                        Gfx.DrawString
                        (
                            text: songParts[songPartIndex],
                            font: ItalicFont,
                            brush: XBrushes.Black,
                            layoutRectangle: new XRect
                            (
                                x: Page.Width.Value / 5 * i + 5,
                                y: Page.Height.Value / 8 * (2 + j) - XUnit.FromPoint(FontRegularSize) * ((songParts.Length - 1) * 0.5 - songPartIndex) * LineHeight,
                                width: Page.Width.Value / 5 - 5,
                                height: Page.Height.Value / 8
                            ),
                            format: XStringFormats.Center
                        );
                    }

                    for (var artistPartIndex = 0; artistPartIndex < artistParts.Length; artistPartIndex++)
                    {
                        Gfx.DrawString
                        (
                            text: artistParts[artistPartIndex],
                            font: Font,
                            brush: XBrushes.Black,
                            layoutRectangle: new XRect
                            (
                                Page.Width.Value / 5 * i + 5,
                                Page.Height.Value / 8 * (2 + j)
                                - XUnit.FromPoint(FontRegularSize) * (((songParts.Length + artistParts.Length) - 1) * 0.5 - (artistPartIndex + songParts.Length + 2)) * LineHeight,
                                Page.Width.Value / 5 - 5,
                                Page.Height.Value / 8
                            ),
                            format: XStringFormats.Center
                        );
                    }
                }
            }

            //Draw table borders
            var pen = new XPen(XColors.Black, TableBorder);
            for (var i = 0; i <= 5; i++)
            {
                Gfx.DrawLine
                (
                    pen,
                    Page.Width.Value / 5 * i,
                    Page.Height.Value / 8 * 2,
                    Page.Width.Value / 5 * i,
                    Page.Height.Value / 8 * 7
                );
                Gfx.DrawLine
                (
                    pen,
                    0,
                    Page.Height.Value / 8 * (2 + i),
                    Page.Width,
                    Page.Height.Value / 8 * (2 + i)
                );
            }

            return this;
        }

        public void SavePdf(string filename = "bingo.pdf")
        {
            try
            {
                InternalDocument.Save(filename);
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine($"Cannot save to {filename}.");
                System.Console.Error.WriteLine(e.ToString());
            }
        }

        private List<(string, string)> ArtistSong { get; set; }
        private Random Rand { get; set; }

        private PdfDocument InternalDocument { get; set; }
        private PdfPage Page { get; set; }
        private XGraphics Gfx { get; set; }
        private XFont Font { get; }
        private XFont TitleFont { get; }

        private const double FontRegularSize = 8;
        private const double FontTitleSize = 20;
        private const double LineHeight = 1;
        private const double TableBorder = 1.0;

        public void AddWord(string song, string artist)
        {
            ArtistSong.Add((artist, song));
        }
    }
}