// #nullable enable

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace App;

public class WebService
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _serializerOptions;

    public class ResponseItem
    {
        public int frameCount { get; set; }
        public string error { get; set; }
        public List<Result> result { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(frameCount)}: {frameCount}, {nameof(error)}: {error}, {nameof(result)}: {(result != null ? String.Join(", ", result) : null)}";
        }

        public class Result : INotifyPropertyChanged
        {
            public int anilist { get; set; }
            public string filename { get; set; }
            public string episode { get; set; }
            public float from { get; set; }
            public float to { get; set; }
            public double similarity { get; set; }
            public string video { get; set; }
            public string image { get; set; }

            private bool _isImage = true;

            public bool isImage
            {
                get => _isImage;
                set
                {
                    var isChanged = SetField(ref _isImage, value);
                    if (isChanged) OnPropertyChanged(nameof(isVideo));
                }
            }

            public bool isVideo => !_isImage;

            public string start
            {
                get
                {
                    var ts = TimeSpan.FromSeconds(from);
                    var endTimeSpan = TimeSpan.FromSeconds(to);
                    return endTimeSpan.Hours > 0 ? $"{ts.Hours:D2}:" : "" + $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                }
            }

            public string end
            {
                get
                {
                    var ts = TimeSpan.FromSeconds(to);
                    return ts.Hours > 0 ? $"{ts.Hours:D2}:" : "" + $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                }
            }

            public override string ToString()
            {
                return
                    $"{nameof(anilist)}: {anilist}, {nameof(filename)}: {filename}, {nameof(episode)}: {episode}, {nameof(from)}: {from}, {nameof(to)}: {to}, {nameof(similarity)}: {similarity}, {nameof(video)}: {video}, {nameof(image)}: {image}";
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }
    }


    public WebService()
    {
        _client = new HttpClient();
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        _serializerOptions.Converters.Add(new StringConverter());
    }

    private class StringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                {
                    var arrayString = new StringBuilder("[");
                    reader.Read();
                    while (reader.TokenType != JsonTokenType.EndArray)
                    {
                        arrayString.Append(reader.TokenType == JsonTokenType.String
                            ? reader.GetString()
                            : GetRawPropertyValue(reader));
                        arrayString.Append(", ");
                        reader.Read();
                    }

                    return arrayString.Remove(arrayString.Length - 2, 2).Append(']').ToString();
                }
                case JsonTokenType.String:
                    return reader.GetString();
                default:
                    return GetRawPropertyValue(reader);
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        private static string? GetRawPropertyValue(Utf8JsonReader jsonReader)
        {
            var utf8Bytes =
                jsonReader.HasValueSequence ? jsonReader.ValueSequence.ToArray() : jsonReader.ValueSpan;
            return Encoding.UTF8.GetString(utf8Bytes);
        }
    }

    public async Task<ResponseItem> SearchByImageUrl(string url, Page page)
    {
        var item = new ResponseItem();
        var uri = new Uri(string.Format(IConstants.GetUrl, UrlEncoder.Default.Encode(url)));
        try
        {
            var response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                item = JsonSerializer.Deserialize<ResponseItem>(content, _serializerOptions);
            }
        }
        catch (Exception ex)
        {
            // Debug.WriteLine(@"\tERROR {0}", ex.Message);
            await page.DisplayAlert("Error", ex.Message, "OK");
        }

        return item;
    }

    public async Task<ResponseItem> SearchByImageUpload(FileResult pic, Page page)
    {
        var item = new ResponseItem();
        try
        {
            MultipartFormDataContent form = new();
            await using var stream = await pic.OpenReadAsync();
            StreamContent streamContent = new(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                FileName = pic.FileName,
                Name = pic.FileName[..pic.FileName.LastIndexOf('.')]
            };
            form.Add(streamContent);
            var response = await _client.PostAsync(IConstants.PostUrl, form);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                item = JsonSerializer.Deserialize<ResponseItem>(content, _serializerOptions);
            }
        }
        catch (Exception ex)
        {
            // Debug.WriteLine(@"\tERROR {0}", ex.Message);
            await page.DisplayAlert("Error", ex.Message, "OK");
        }

        return item;
    }


    private interface IConstants
    {
        public const string GetUrl = "https://api.trace.moe/search?url={0}";
        public const string PostUrl = "https://api.trace.moe/search";
    }
}