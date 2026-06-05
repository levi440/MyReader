using System.Diagnostics;

namespace MyReader.Services;

/// <summary>
/// 音频播放服务（简化版，使用 Process 调用系统播放器）
/// </summary>
public class AudioPlayerService
{
    private Process? _playerProcess;
    private string? _currentUrl;
    private bool _isPlaying;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsPlaying => _isPlaying;
    public string? CurrentUrl => _currentUrl;

    /// <summary>
    /// 播放音频 URL
    /// </summary>
    public void Play(string audioUrl)
    {
        try
        {
            Stop();

            _currentUrl = audioUrl;
            _isPlaying = true;

            // 使用系统默认播放器打开
            _playerProcess = new Process();
            _playerProcess.StartInfo = new ProcessStartInfo
            {
                FileName = audioUrl,
                UseShellExecute = true
            };
            _playerProcess.EnableRaisingEvents = true;
            _playerProcess.Exited += (s, e) =>
            {
                _isPlaying = false;
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            };
            _playerProcess.Start();

            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _isPlaying = false;
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop()
    {
        try
        {
            if (_playerProcess != null && !_playerProcess.HasExited)
            {
                _playerProcess.Kill();
            }
            _playerProcess?.Dispose();
            _playerProcess = null;
        }
        catch { }

        _isPlaying = false;
        _currentUrl = null;
    }

    /// <summary>
    /// 打开音频文件（本地文件）
    /// </summary>
    public void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            Play(filePath);
        }
        else
        {
            ErrorOccurred?.Invoke(this, "文件不存在");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
