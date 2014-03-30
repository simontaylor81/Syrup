﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using SlimDX;
using ShaderEditorApp.ViewModel;

namespace SRPRendering
{
	// Class that handles camera controls for a viewport.
	class Camera
	{
		public delegate void CameraMovedHandler(Camera camera);
		public event CameraMovedHandler Moved;

		// Register a control to get input from, and be invalidated when the camera moves.
		public void RegisterViewport(Control control)
		{
			// Hook the control's mouse events.
			control.MouseMove += MouseMove;
			control.MouseLeave += MouseLeave;
			control.MouseUp += MouseUp;
			control.MouseDown += MouseDown;
			control.MouseWheel += MouseWheel;

			// Register event to invalidate the control when the mouse moves.
			Moved += (c) => { control.Invalidate(); };
		}

		// Get the world -> view space matrix.
		public Matrix WorldToViewMatrix
		{
			get
			{
				return
					Matrix.Translation(-pos) *
					Matrix.RotationY(-yaw) *
					Matrix.RotationX(-pitch);
			}
		}

		// Get the eye position.
		public Vector3 EyePosition { get { return pos; } }

		// Get view -> projection matrix. Aspect ratio differs per viewport so must be passed as a param.
		public Matrix GetViewToProjectionMatrix(float aspectRatio)
		{
			return Matrix.PerspectiveFovLH(FOV * (float)Math.PI / 180.0f, aspectRatio, Near, Far);
		}

		public ViewportViewModel ViewportViewModel { get; set; }


		//------------------------------------------------------------------------------------------
		// Event handlers

		private MouseEventHandler EventDispatch(MouseEventHandler orbitHandler, MouseEventHandler walkHandler)
		{
			return (o, e) =>
				{
					switch (ViewportViewModel.SelectedCameraMode)
					{
						case ViewportViewModel.CameraMode.Orbit:
							orbitHandler(o, e);
							break;

						case ViewportViewModel.CameraMode.Walk:
							walkHandler(o, e);
							break;
					}
				};
		}

		private void MouseMove(object sender, MouseEventArgs e)
		{
			//Console.WriteLine("MouseMove, Buttons = ", e.Button.ToString());
			if (bDragging)
			{
				var deltaX = e.Location.X - dragStart.X;
				var deltaY = e.Location.Y - dragStart.Y;

				switch (ViewportViewModel.SelectedCameraMode)
				{
					case ViewportViewModel.CameraMode.Orbit:
						MouseDrag_Orbit(deltaX, deltaY, e.Button);
						break;

					case ViewportViewModel.CameraMode.Walk:
						MouseDrag_Walk(deltaX, deltaY, e.Button);
						break;
				}

				dragStart = e.Location;
				Moved(this);
			}
		}

		private void MouseDrag_Orbit(int deltaX, int deltaY, MouseButtons buttons)
		{
			if (buttons == MouseButtons.Left)
			{
				var orbitFocus = pos + LookDir * orbitRadius;

				yaw = (yaw + (float)deltaX * 0.01f) % (2.0f * (float)Math.PI);
				if (yaw < 0.0f)
					yaw += 2.0f * (float)Math.PI;

				pitch += (float)deltaY * 0.01f;
				pitch = Math.Min(Math.Max(pitch, (float)-Math.PI * 0.5f), (float)Math.PI * 0.5f);

				pos = orbitFocus - LookDir * orbitRadius;
			}
			else if (buttons == MouseButtons.Right)
			{
				var deltaRadius = (float)deltaY * 0.02f;
				orbitRadius += deltaRadius;
				pos -= deltaRadius * LookDir;
			}
			else if (buttons == MouseButtons.Middle || buttons == (MouseButtons.Left | MouseButtons.Right))
			{
				float deltaRight = (float)deltaX * -0.01f;
				float deltaUp = (float)deltaY * 0.01f;

				var viewToWorldMatrix = WorldToViewMatrix;
				viewToWorldMatrix.Invert();
				var right = Vector3.TransformNormal(new Vector3(1.0f, 0.0f, 0.0f), viewToWorldMatrix);
				var up = Vector3.TransformNormal(new Vector3(0.0f, 1.0f, 0.0f), viewToWorldMatrix);

				pos += deltaRight * right + deltaUp * up;
			}
		}

		private void MouseDrag_Walk(int deltaX, int deltaY, MouseButtons buttons)
		{
			if (buttons == MouseButtons.Left)
			{
				// Unreal style -- move in horizontal plane.
				float deltaForward = (float)deltaY * -0.01f;
				var forward = new Vector3(
					(float)Math.Sin(yaw),
					0.0f,
					(float)Math.Cos(yaw));
				pos += deltaForward * forward;

				yaw = (yaw + (float)deltaX * 0.01f) % (2.0f * (float)Math.PI);
				if (yaw < 0.0f)
					yaw += 2.0f * (float)Math.PI;
			}
			else if (buttons == MouseButtons.Right)
			{
				yaw = (yaw + (float)deltaX * 0.01f) % (2.0f * (float)Math.PI);
				if (yaw < 0.0f)
					yaw += 2.0f * (float)Math.PI;

				pitch += (float)deltaY * 0.01f;
				pitch = Math.Min(Math.Max(pitch, (float)-Math.PI * 0.5f), (float)Math.PI * 0.5f);
			}
			else if (buttons == MouseButtons.Middle || buttons == (MouseButtons.Left | MouseButtons.Right))
			{
				float deltaRight = (float)deltaX * 0.01f;
				float deltaUp = (float)deltaY * -0.01f;

				var viewToWorldMatrix = WorldToViewMatrix;
				viewToWorldMatrix.Invert();
				var right = Vector3.TransformNormal(new Vector3(1.0f, 0.0f, 0.0f), viewToWorldMatrix);
				var up = Vector3.TransformNormal(new Vector3(0.0f, 1.0f, 0.0f), viewToWorldMatrix);

				pos += deltaRight * right + deltaUp * up;
			}
		}


		private void MouseLeave(object sender, EventArgs e)
		{
			bDragging = false;
		}

		private void MouseUp(object sender, MouseEventArgs e)
		{
			if (Control.MouseButtons == MouseButtons.None)
				bDragging = false;
		}

		private void MouseDown(object sender, MouseEventArgs e)
		{
			if (!bDragging)
			{
				bDragging = true;
				dragStart = e.Location;
			}
		}

		private void MouseWheel(object sender, MouseEventArgs e)
		{
			var deltaRadius = (float)e.Delta * -0.005f;
			pos -= deltaRadius * LookDir;

			if (ViewportViewModel.SelectedCameraMode == ViewportViewModel.CameraMode.Orbit)
				orbitRadius += deltaRadius;

			Moved(this);
		}

		private Vector3 LookDir
		{
			get
			{
				float y = (float)Math.Sin(-pitch);
				var xz = Math.Cos(-pitch);

				float x = (float) (xz * Math.Sin(yaw));
				float z = (float) (xz * Math.Cos(yaw));

				return new Vector3(x, y, z);
			}
		}

		// Projection parameters.
		public float FOV = 45.0f;
		public float Near = 1.0f;
		public float Far = 100.0f;

		// View parameters.
		private float yaw = 0.0f;
		private float pitch = 0.0f;
		private Vector3 pos = new Vector3(0.0f, 0.0f, -5.0f);
		private float orbitRadius = 5.0f;

		// Mouse handling variables.
		private bool bDragging = false;
		private Point dragStart = new Point();
	}
}