// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneCircularArcBoundingBox : FrameworkTestScene
    {
        private SmoothPath path;
        private Box boundingBox;

        private readonly BindableList<Vector2> controlPoints = new BindableList<Vector2>();

        private float startAngle, endAngle, radius;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                boundingBox = new Box
                {
                    RelativeSizeAxes = Axes.None,
                    Colour = Color4.Red
                },
                path = new SmoothPath
                {
                    Colour = Color4.White,
                    PathRadius = 2
                }
            };

            AddSliderStep("starting angle", 0, 360, 90, angle =>
            {
                startAngle = angle;
                generateControlPoints();
            });

            AddSliderStep("end angle", 0, 360, 270, angle =>
            {
                endAngle = angle;
                generateControlPoints();
            });

            AddSliderStep("radius", 1, 300, 150, radius =>
            {
                this.radius = radius;
                generateControlPoints();
            });
        }

        protected override void LoadComplete()
        {
            controlPoints.BindCollectionChanged((_, __) =>
            {
                var copy = controlPoints.ToArray();
                if (copy.Length != 3)
                    return;

                path.Vertices = PathApproximator.ApproximateCircularArc(copy);

                var bounds = PathApproximator.CircularArcBoundingBox(copy);
                boundingBox.Size = bounds.Size;
            });
        }

        private void generateControlPoints()
        {
            float midpoint = (startAngle + endAngle) / 2;

            Vector2 polarToCartesian(float r, float theta) =>
                new Vector2(
                    r * MathF.Cos(MathHelper.DegreesToRadians(theta)),
                    r * MathF.Sin(MathHelper.DegreesToRadians(theta)));

            controlPoints.Clear();
            controlPoints.AddRange(new[]
            {
                polarToCartesian(radius, startAngle),
                polarToCartesian(radius, midpoint),
                polarToCartesian(radius, endAngle)
            });
        }
    }
}
