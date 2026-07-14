using System.Net;
using System.Net.Sockets;

namespace Teatime.Configuration;

public static class PortAvailabilityChecker
{
    // Probes each configured URL's port before Kestrel binds to it, so a conflict fails fast
    // with a clear message instead of an opaque exception from app.Run().
    public static bool TryEnsureUrlsAvailable(IEnumerable<string> urls, out int conflictingPort)
    {
        foreach (var rawUrl in urls)
        {
            if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri)) continue;
            if (uri.Scheme is not ("http" or "https")) continue;

            var address = uri.Host is "localhost" or "0.0.0.0" or "*" or "+"
                ? IPAddress.Loopback
                : IPAddress.Parse(uri.Host);

            try
            {
                using var probe = new TcpListener(address, uri.Port);
                probe.Start();
                probe.Stop();
            }
            catch (SocketException)
            {
                conflictingPort = uri.Port;
                return false;
            }
        }

        conflictingPort = 0;
        return true;
    }
}
