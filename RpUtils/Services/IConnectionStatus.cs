
using RpUtils.Models;
using System;
using System.Threading.Tasks;

namespace RpUtils.Services;

public interface IConnectionStatus
{
    ConnectionState Status { get; }
    event Action<ConnectionState>? OnStatusChanged;
    Task ConnectAsync();
    Task DisconnectAsync();
}
