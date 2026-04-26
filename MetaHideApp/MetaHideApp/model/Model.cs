using System;
using System.Collections.Generic;
using System.IO;
using test;

namespace MetaHide.model;

public class Model : ISteganography
{
    private readonly List<ISteganography> _handlers;
    private ISteganography _activeHandler;
    private bool _hiddenMode = false;

    public Model()
    {
        _handlers = new List<ISteganography>
        {
            new JpegSteganography(),
            new PngSteganography()
        };
    }

    public void SetHiddenMode(bool hidden)
    {
        _hiddenMode = hidden;
        _activeHandler?.SetHiddenMode(hidden);
    }

    public int GetCurrentFieldId()
    {
        return _activeHandler?.GetCurrentFieldId() ?? 0;
    }

    public (bool success, string message, string outputPath) HideData(string imagePath, string data)
    {
        _activeHandler = GetHandler(imagePath);
        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла (только .jpg, .jpeg, .png)", null);

        _activeHandler.SetHiddenMode(_hiddenMode);
        return _activeHandler.HideData(imagePath, data);
    }

    public (bool success, string message, string data) ExtractData(string imagePath)
    {
        _activeHandler = GetHandler(imagePath);
        if (_activeHandler == null)
            return (false, "Неподдерживаемый формат файла", null);

        _activeHandler.SetHiddenMode(_hiddenMode);
        return _activeHandler.ExtractData(imagePath);
    }

    public bool HasHiddenData(string imagePath)
    {
        var handler = GetHandler(imagePath);
        if (handler == null) return false;
        handler.SetHiddenMode(_hiddenMode);
        return handler.HasHiddenData(imagePath);
    }

    public string GetAllExifFields(string imagePath)
    {
        var handler = GetHandler(imagePath);
        if (handler == null) return "Неподдерживаемый формат файла";
        handler.SetHiddenMode(_hiddenMode);
        return handler.GetAllExifFields(imagePath);
    }

    public bool SupportsFormat(string filePath)
    {
        return GetHandler(filePath) != null;
    }

    private ISteganography GetHandler(string filePath)
    {
        foreach (var handler in _handlers)
            if (handler.SupportsFormat(filePath))
                return handler;
        return null;
    }
}