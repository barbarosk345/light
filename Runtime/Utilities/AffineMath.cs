// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Niantic.Lightship.AR.Utilities
{
  /// This utility is a collection of functions that produce 2D affine transformations.
  /// Affine transformation is a linear mapping method that preserves points, straight
  /// lines, and planes. The functions here are used to calculate matrices that fit
  /// images to the viewport.
  internal static class AffineMath
  {
    /// Returns an affine transformation such that if multiplied with normalized
    /// coordinates in the target coordinate frame, the results are normalized
    /// coordinates in the source coordinate frame.
    internal static Matrix4x4 Fit
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      float targetWidth,
      float targetHeight,
      ScreenOrientation targetOrientation,
      bool reverseRotation = false
    )
    {
      var rotatedContainer = RotateResolution
        (sourceWidth, sourceHeight, sourceOrientation, targetOrientation);

      // Calculate scaling
      var targetRatio = targetWidth / targetHeight;
      var s = targetRatio < 1.0f
        ? new Vector2(targetWidth / (targetHeight / rotatedContainer.y * rotatedContainer.x), 1.0f)
        : new Vector2(1.0f, targetHeight / (targetWidth / rotatedContainer.x * rotatedContainer.y));

      var rotate = reverseRotation
          ? ScreenRotation(from: targetOrientation, to: sourceOrientation)
          : ScreenRotation(from: sourceOrientation, to: targetOrientation);
      var scale = Scaling(s);
      var translate = Translation(new Vector2((1.0f - s.x) * 0.5f, (1.0f - s.y) * 0.5f));

      return scale * translate * rotate;
    }

    /// Produces a 2D rotation matrix that preserves parallel lines
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Matrix4x4 Rotation(double rad)
    {
      return new Matrix4x4
      (
        new Vector4((float) Math.Cos(rad), (float) -Math.Sin(rad), 0, 0),
        new Vector4((float) Math.Sin(rad), (float) Math.Cos(rad), 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }

    /// Produces a 2D translation matrix that preserves parallel lines
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Matrix4x4 Translation(Vector2 translation)
    {
      return new Matrix4x4
      (
        new Vector4(1, 0, translation.x, 0),
        new Vector4(0, 1, translation.y, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0,0, 1)
      );
    }

    /// Produces a 2D scaling matrix that preserves parallel lines
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Matrix4x4 Scaling(Vector2 scale)
    {
      return new Matrix4x4
      (
        new Vector4(scale.x, 0, 0, 0),
        new Vector4(0, scale.y, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 RotateResolution
    (
      float sourceWidth,
      float sourceHeight,
      ScreenOrientation sourceOrientation,
      ScreenOrientation targetOrientation
    )
    {
      if (sourceOrientation == ScreenOrientation.LandscapeLeft)
      {
        return
          targetOrientation == ScreenOrientation.LandscapeLeft ||
          targetOrientation == ScreenOrientation.LandscapeRight
            ? new Vector2(sourceWidth, sourceHeight)
            : new Vector2(sourceHeight, sourceWidth);
      }

      return
        targetOrientation == ScreenOrientation.Portrait ||
        targetOrientation == ScreenOrientation.PortraitUpsideDown
          ? new Vector2(sourceWidth, sourceHeight)
          : new Vector2(sourceHeight, sourceWidth);
    }

    /// Calculates an affine transformation to rotate from one screen orientation to another
    /// around the pivot.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns An affine matrix to be applied to normalized image coordinates.
    private static Matrix4x4 ScreenRotation(ScreenOrientation from, ScreenOrientation to)
    {
      // Rotate around the center
      return Translation(-s_center) * Rotation(GetRadians(from, to)) * Translation(s_center);
    }

    /// Calculates the angle to rotate from one screen orientation to another in radians.
    /// @param from Original orientation.
    /// @param to Target orientation.
    /// @returns Angle to rotate to get from one orientation to the other.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double GetRadians(ScreenOrientation from, ScreenOrientation to)
    {
      const double rotationUnit = Math.PI / 2.0;
      return (s_screenOrientationLookup[to] - s_screenOrientationLookup[from]) * rotationUnit;
    }

    #region Constants

    /// Screen orientation to rotation id
    private static readonly IDictionary<ScreenOrientation, int> s_screenOrientationLookup =
      new Dictionary<ScreenOrientation, int>
      {
        {
          ScreenOrientation.LandscapeLeft, 0
        },
        {
          ScreenOrientation.Portrait, 1
        },
        {
          ScreenOrientation.LandscapeRight, 2
        },
        {
          ScreenOrientation.PortraitUpsideDown, 3
        }
      };

    /// Matrix to invert an UV vertically
    internal static readonly Matrix4x4 s_invertVertical
      = new(
        new Vector4(1, 0, 0, 0),
        new Vector4(0, -1, 1, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );

    /// Matrix to invert an UV horizontally
    internal static readonly Matrix4x4 s_invertHorizontal
      = new(
        new Vector4(-1, 0, 1, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(0, 0, 0, 1)
      );

    private static readonly Vector2 s_center = new(0.5f, 0.5f);

    #endregion
  }
}
