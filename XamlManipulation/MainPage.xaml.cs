using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.Canvas;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using System.Threading.Tasks;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas.Effects;

namespace XamlManipulation
{
    public sealed partial class MainPage : Page
    {
        public float sliderWidth => 512;
        public float sliderHeight => 128;
        public float strokeThickness => 10;
        public Color BackColor => Colors.DarkSlateGray;
        public Color ButtonColor => Colors.CadetBlue;
        public Color TapColor => Colors.Aquamarine;
        double totalDelta;
        double position;
        Compositor _compositor;
        Visual myVisual;
        ContainerVisual _container;
        CanvasDevice _canvasDevice;
        event TypedEventHandler<CompositionGraphicsDevice, RenderingDeviceReplacedEventArgs> RenderingDeviceReplaced;
        CompositionGraphicsDevice _graphicsDevice;
        CompositionDrawingSurface lineDrawingSurface;
        CompositionDrawingSurface buttonDrawingSurface;
        CompositionDrawingSurface backgroundDrawingSurface;
        CompositionDrawingSurface tapDrawingSurface;
        CompositionSurfaceBrush backgroundSurfaceBrush;
        CompositionSurfaceBrush butttonSurfaceBrush;
        CompositionSurfaceBrush tapSurfaceBrush;
        CompositionSurfaceBrush lineSurfaceBrush;
        CompositionMaskBrush backgroundMaskBrush;
        CompositionMaskBrush tapMaskBrush;
        CompositionMaskBrush lineMaskBrush;
        CompositionColorBrush backColorBrush;
        CompositionEffectBrush lineEffeectBrush;
        CompositionEffectBrush tapEffectBrush;
        CompositionEffectFactory tapEffectFactory;
        CompositionEffectFactory lineEffectFactory;
        GaussianBlurEffect lineBlurEffect;
        GaussianBlurEffect tapBlurEffect;
        SpriteVisual buttonSprite;
        SpriteVisual backgroundSprite;
        SpriteVisual lineSprite;
        SpriteVisual tapSprite;
        Rect rect;
        Vector3KeyFrameAnimation buttonAnimationRight;
        Vector3KeyFrameAnimation buttonAnimationLeft;
        Vector2KeyFrameAnimation lineEffectAnimation;
        Vector2KeyFrameAnimation tapEffectAnimation;
        ScalarKeyFrameAnimation tapExitedEffectAnimation;
        ScalarKeyFrameAnimation blurAnimation;
        ExpressionAnimation moveAnimation;
        CubicBezierEasingFunction buttonEase;
        CubicBezierEasingFunction lineEase;
        CompositionScopedBatch myScopedBatch;
        List<float> posList = new List<float>();
        float minWidth;
        public MainPage()
        {
            this.InitializeComponent();
            myVisual = ElementCompositionPreview.GetElementVisual(buttonRectangle);
            _compositor = myVisual.Compositor;
            CreateAnimation();
            CreateDevice();
            CreateSpriteVisual();
            LineBlurEffect();
            MoiseBlurEffect();
            _container = _compositor.CreateContainerVisual();
            _container.Children.InsertAtTop(backgroundSprite);
            _container.Children.InsertAtTop(lineSprite);
            _container.Children.InsertAtTop(buttonSprite);
            _container.Children.InsertAtTop(tapSprite);
            ElementCompositionPreview.SetElementChildVisual(CanvasVisual, _container);
            minWidth = sliderHeight - 2 * strokeThickness;
        }
        public void Redraw()
        {
            using (var session = CanvasComposition.CreateDrawingSession(buttonDrawingSurface))
            {
                session.Clear(Colors.Transparent);
                session.FillRoundedRectangle(rect, sliderHeight / 2, sliderHeight / 2, ButtonColor);
            }
        }
        private void CreateSpriteVisual()
        {
            backgroundDrawingSurface = _graphicsDevice.CreateDrawingSurface(
                    new Size(sliderWidth, sliderHeight),
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    DirectXAlphaMode.Premultiplied);
            using (var session = CanvasComposition.CreateDrawingSession(backgroundDrawingSurface))
            {
                Rect rect = new Rect();
                rect.Width = sliderWidth;
                rect.Height = sliderHeight;
                session.Clear(Colors.Transparent);
                session.FillRoundedRectangle(rect, sliderHeight / 2, sliderHeight / 2, Colors.White);
            }
            backColorBrush = _compositor.CreateColorBrush(BackColor);
            backgroundSurfaceBrush = _compositor.CreateSurfaceBrush(backgroundDrawingSurface);
            backgroundMaskBrush = _compositor.CreateMaskBrush();

            backgroundMaskBrush.Source = backColorBrush;
            backgroundMaskBrush.Mask = backgroundSurfaceBrush;
            backgroundSprite = _compositor.CreateSpriteVisual();
            backgroundSprite.Size = new Vector2(sliderWidth, sliderHeight);
            backgroundSprite.Brush = backgroundMaskBrush;

            buttonDrawingSurface = _graphicsDevice.CreateDrawingSurface(
                new Size(sliderWidth - 2 * strokeThickness, sliderHeight - 2 * strokeThickness),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
            using (var session = CanvasComposition.CreateDrawingSession(buttonDrawingSurface))
            {
                rect.Width = sliderHeight - 2 * strokeThickness;
                rect.Height = sliderHeight - 2 * strokeThickness;
                session.Clear(Colors.Transparent);
                session.FillRoundedRectangle(rect, sliderHeight / 2- 2 * strokeThickness, sliderHeight / 2 - 2 * strokeThickness, ButtonColor);
            }
            Redraw();
            butttonSurfaceBrush = _compositor.CreateSurfaceBrush(buttonDrawingSurface);
            buttonSprite = _compositor.CreateSpriteVisual();
            buttonSprite.Size = new Vector2(sliderWidth - 2 * strokeThickness, sliderHeight - 2 * strokeThickness);
            buttonSprite.Brush = butttonSurfaceBrush;

            buttonSprite.Offset = new Vector3(strokeThickness, strokeThickness, 0);
        }
        private void LineBlurEffect()
        {
            lineBlurEffect = new GaussianBlurEffect()
            {
                Name = "Blur",
                Source = new CompositionEffectSourceParameter("source"),
                BlurAmount = 6f,
                BorderMode = EffectBorderMode.Hard,
            };
            lineEffectFactory = _compositor.CreateEffectFactory(lineBlurEffect);
            lineEffeectBrush = lineEffectFactory.CreateBrush();
            lineDrawingSurface = _graphicsDevice.CreateDrawingSurface(
            new Size(sliderWidth, sliderHeight),
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            DirectXAlphaMode.Premultiplied);
            using (var session = CanvasComposition.CreateDrawingSession(lineDrawingSurface))
            {
                Rect rect = new Rect();
                rect.Width = sliderHeight / 2;
                rect.Height = sliderHeight;
                session.FillRectangle(rect, Color.FromArgb(127, 255, 255, 255));
            }
            lineSurfaceBrush = _compositor.CreateSurfaceBrush(lineDrawingSurface);
            lineEffeectBrush.SetSourceParameter("source", lineSurfaceBrush);
            lineMaskBrush = _compositor.CreateMaskBrush();
            lineSprite = _compositor.CreateSpriteVisual();
            lineSprite.Size = new Vector2(sliderWidth, sliderHeight);
            lineMaskBrush.Source = lineEffeectBrush;
            lineMaskBrush.Mask = backgroundSurfaceBrush;
            lineSprite.Brush = lineMaskBrush;
            lineSurfaceBrush.StartAnimation("Offset", lineEffectAnimation);
        }
        private void MoiseBlurEffect()
        {
            tapBlurEffect = new GaussianBlurEffect()
            {
                Name = "Blur",
                Source = new CompositionEffectSourceParameter("sourcePoint"),
                BorderMode = EffectBorderMode.Hard,
            };
            tapEffectFactory = _compositor.CreateEffectFactory(tapBlurEffect, new[] { "Blur.BlurAmount" });

            tapDrawingSurface = _graphicsDevice.CreateDrawingSurface(
               new Size(sliderWidth, sliderHeight),
               DirectXPixelFormat.B8G8R8A8UIntNormalized,
               DirectXAlphaMode.Premultiplied);
            using (var session = CanvasComposition.CreateDrawingSession(tapDrawingSurface))
            {
                session.FillCircle(new Vector2(sliderWidth / 2, sliderHeight / 2), 8, TapColor);
            }
            tapSurfaceBrush = _compositor.CreateSurfaceBrush(tapDrawingSurface);
            tapSurfaceBrush.AnchorPoint = new Vector2(0.5f, 0.5f);

            tapEffectBrush = tapEffectFactory.CreateBrush();
            tapEffectBrush.SetSourceParameter("sourcePoint", tapSurfaceBrush);

            tapMaskBrush = _compositor.CreateMaskBrush();
            tapMaskBrush.Source = tapEffectBrush;
            tapMaskBrush.Mask = butttonSurfaceBrush;           
           
            tapSprite = _compositor.CreateSpriteVisual();
            tapSprite.Size = new Vector2(sliderWidth - 2 * strokeThickness, sliderHeight - 2 * strokeThickness);
            tapSprite.Brush = tapMaskBrush;
            tapSprite.Offset = new Vector3(strokeThickness, strokeThickness, 0);
        }
        private void CreateAnimation()
        {
            moveAnimation = _compositor.CreateExpressionAnimation();
            moveAnimation.Expression = "myVisual.Offset.X";
            moveAnimation.SetReferenceParameter("myVisual", myVisual);

            //easein
            buttonEase = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0.0f), new Vector2(0.58f, 1.0f));
            buttonAnimationRight = _compositor.CreateVector3KeyFrameAnimation();
            buttonAnimationRight.InsertKeyFrame(1.0f, new Vector3(sliderWidth - sliderHeight, 0.0f, 0.0f), buttonEase);
            buttonAnimationRight.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
            buttonAnimationRight.Duration = TimeSpan.FromMilliseconds(500);

            buttonAnimationLeft = _compositor.CreateVector3KeyFrameAnimation();
            buttonAnimationLeft.InsertKeyFrame(1.0f, new Vector3(0), buttonEase);
            buttonAnimationLeft.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
            buttonAnimationLeft.Duration = TimeSpan.FromMilliseconds(500);

            lineEase = _compositor.CreateCubicBezierEasingFunction(new Vector2(.47f, .01f), new Vector2(.81f, .29f));
            lineEffectAnimation = _compositor.CreateVector2KeyFrameAnimation();
            lineEffectAnimation.InsertKeyFrame(0.0f, new Vector2(-sliderHeight, 0.0f));
            lineEffectAnimation.InsertKeyFrame(0.4f, new Vector2(0), lineEase);
            lineEffectAnimation.InsertKeyFrame(0.6f, new Vector2(sliderWidth, 0.0f), lineEase);
            lineEffectAnimation.InsertKeyFrame(1.0f, new Vector2(sliderWidth, 0.0f));
            lineEffectAnimation.Duration = TimeSpan.FromSeconds(2);
            lineEffectAnimation.IterationBehavior = AnimationIterationBehavior.Forever;           

            tapEffectAnimation = _compositor.CreateVector2KeyFrameAnimation();
            tapEffectAnimation.InsertKeyFrame(0.0f, new Vector2(1));
            tapEffectAnimation.InsertKeyFrame(1.0f, new Vector2(60));
            tapEffectAnimation.Duration = TimeSpan.FromSeconds(2);

            tapExitedEffectAnimation = _compositor.CreateScalarKeyFrameAnimation();
            tapExitedEffectAnimation.InsertKeyFrame(0.0f, 1.0f);
            tapExitedEffectAnimation.InsertKeyFrame(1.0f, 0.0f);
            tapExitedEffectAnimation.Duration = TimeSpan.FromMilliseconds(300);

            blurAnimation = _compositor.CreateScalarKeyFrameAnimation();
            blurAnimation.InsertKeyFrame(0.0f, 0.0f);
            blurAnimation.InsertKeyFrame(1.0f, 30.0f);
            blurAnimation.Duration = TimeSpan.FromSeconds(1);
        }
        private void DeviceLost(CanvasDevice sender, object args)
        {
            Debug.WriteLine("CompositionImageLoader - Canvas Device Lost");
            sender.DeviceLost -= DeviceLost;

            _canvasDevice = CanvasDevice.GetSharedDevice();
            _canvasDevice.DeviceLost += DeviceLost;

            CanvasComposition.SetCanvasDevice(_graphicsDevice, _canvasDevice);
        }
        private void CreateDevice()
        {
            if (_compositor != null)
            {
                if (_canvasDevice == null)
                {
                    _canvasDevice = CanvasDevice.GetSharedDevice();
                    _canvasDevice.DeviceLost += DeviceLost;
                }

                if (_graphicsDevice == null)
                {
                    _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, _canvasDevice);
                    _graphicsDevice.RenderingDeviceReplaced += RenderingDeviceReplaced;
                }
            }
        }
        public void Rectangle_ManipulationDelta_1(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            totalDelta += e.Delta.Translation.X;
            position = totalDelta;
            if (position > sliderWidth - sliderHeight )
            {
                position = sliderWidth - sliderHeight ;
            }
            if (position < 0)
            {
                position = 0;
            }
            myVisual.Offset = new Vector3(((float)position), 0, 0);
        }
        private void Rectangle_ManipulationCompleted_1(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            myScopedBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            myScopedBatch.Completed += MyScopedBatch_Completed;
            currentTime = 0;
           
            animatedControl.Paused = false;
            flag = true;
            //flag = true;
            //rect.Width = minWidth;
            //  flag = false;
            //rect.X = position;

            if (myVisual.Offset.X >= (sliderWidth - sliderHeight) / 2)
            {
                myVisual.StartAnimation("Offset", buttonAnimationRight);                
            }
            if (myVisual.Offset.X < (sliderWidth - sliderHeight) / 2)
            {
                myVisual.StartAnimation("Offset", buttonAnimationLeft);
            }
            myScopedBatch.End();
        }
        private void MyScopedBatch_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            totalDelta = myVisual.Offset.X;
            position = totalDelta;
            //rect.Width = minWidth;
            //rect.X = posList[0];
        }
        private void Rectangle_ManipulationStarting_1(object sender, ManipulationStartingRoutedEventArgs e)
        {
            flag = false;
            myVisual.StopAnimation("Offset");
            // rect.X = position;
            // position = totalDelta;
        }
        private void CanvasVisual_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            float posX = (float)e.GetCurrentPoint(buttonRectangle).Position.X;
            float posY = (float)e.GetCurrentPoint(buttonRectangle).Position.Y;
            Vector2 positionPoint = new Vector2(posX, posY);
            Vector2 currentPos = new Vector2((float)position, 0);
            tapSurfaceBrush.Offset = positionPoint+currentPos;                       
            tapSurfaceBrush.StartAnimation("Scale", tapEffectAnimation);
            tapEffectBrush.StartAnimation("Blur.BlurAmount", blurAnimation);
            tapSprite.Opacity =1;
        }
        private void tapRectangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            tapSprite.StartAnimation("Opacity", tapExitedEffectAnimation);
        }
        float easeFunc(float t, float b, float c, float d)
        {
            t /= d;
            t--;
            return c * (t * t * t + 1) + b;

        }
        float currentTime;
        float durationTime = 500;
        bool flag = true;
        public void CanvasVisual_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
           // position = totalDelta;
            posList.Add((float)position);
            if (posList.Count == 10) { posList.RemoveAt(0); }

            if (posList[posList.Count - 1] - posList[0] > 0)//right
            {
                rect.Width = minWidth + posList[posList.Count - 1] - posList[0];
                rect.X = posList[0];
            }
            if (posList[posList.Count - 1] - posList[0] < 0)//left
            {
                rect.Width = minWidth - posList[posList.Count - 1] + posList[0];
                rect.X = position;
            }
            //if (rect.Width < minWidth)
            //{
            //    rect.Width = minWidth;
            //}

            if (flag)
            {
                currentTime += args.Timing.ElapsedTime.Milliseconds;
                if (currentTime >= durationTime)
                {
                    flag = false;
                    return;
                }
                if (myVisual.Offset.X >= (sliderWidth - sliderHeight) / 2)
                {
                    position = easeFunc(currentTime, (float)position, (float)((sliderWidth - sliderHeight) - position), durationTime);
                }
                if (myVisual.Offset.X < (sliderWidth - sliderHeight) / 2)
                {
                    position = easeFunc(currentTime, (float)position, -(float)position, durationTime);
                }
            };
            Redraw();
        }
    }
}



