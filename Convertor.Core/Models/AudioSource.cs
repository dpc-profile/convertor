using YoutubeExplode.Videos.Streams;

namespace Convertor.Core.Models;

public record AudioSource(
    IStreamInfo Stream,
    string Container,
    long BitrateBps);