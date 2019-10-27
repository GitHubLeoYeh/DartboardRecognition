﻿#region Usings

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;

#endregion

namespace DartboardRecognition.Services
{
    public class ThrowService
    {
        private readonly DrawService drawService;
        private readonly Logger logger;
        private readonly List<Ray> rays;
        private readonly Queue<Throw> throwsCollection;

        public ThrowService(DrawService drawService, Logger logger)
        {
            this.logger = logger;
            this.drawService = drawService;
            rays = new List<Ray>();
            throwsCollection = new Queue<Throw>();
        }

        public void CalculateAndSaveThrow()
        {
            logger.Debug($"Calculate throw start");

            if (rays.Count < 2)
            {
                logger.Debug($"Rays count < 2. Calculate throw end.");

                rays.Clear();
                return;
            }

            var firstBestRay = rays.OrderByDescending(i => i.ContourArc).ElementAt(0);
            var secondBestRay = rays.OrderByDescending(i => i.ContourArc).ElementAt(1);
            rays.Clear();

            logger.Debug($"Best rays: '{firstBestRay}' and '{secondBestRay}'");

            var poi = MeasureService.FindLinesIntersection(firstBestRay.CamPoint,
                                                           firstBestRay.RayPoint,
                                                           secondBestRay.CamPoint,
                                                           secondBestRay.RayPoint);
            var anotherThrow = PrepareThrowData(poi);
            throwsCollection.Enqueue(anotherThrow);

            drawService.ProjectionDrawLine(firstBestRay.CamPoint, firstBestRay.RayPoint, true);
            drawService.ProjectionDrawLine(secondBestRay.CamPoint, secondBestRay.RayPoint, false);
            drawService.ProjectionDrawThrow(poi, false);
            drawService.PrintThrow(anotherThrow);

            logger.Debug($"Calculate throw end. Throw:{anotherThrow}");
        }

        private Throw PrepareThrowData(PointF poi)
        {
            var sectors = new List<int>()
                          {
                              14, 9, 12, 5, 20,
                              1, 18, 4, 13, 6,
                              10, 15, 2, 17, 3,
                              19, 7, 16, 8, 11
                          };
            var angle = MeasureService.FindAngle(drawService.projectionCenterPoint, poi);
            var distance = MeasureService.FindDistance(drawService.projectionCenterPoint, poi);
            var sector = 0;
            var multiplier = 1;

            if (distance >= drawService.projectionCoefficent * 95 &&
                distance <= drawService.projectionCoefficent * 105)
            {
                multiplier = 3;
            }
            else if (distance >= drawService.projectionCoefficent * 160 &&
                     distance <= drawService.projectionCoefficent * 170)
            {
                multiplier = 2;
            }

            // Find sector
            if (distance <= drawService.projectionCoefficent * 7)
            {
                sector = 50;
            }
            else if (distance > drawService.projectionCoefficent * 7 &&
                     distance <= drawService.projectionCoefficent * 17)
            {
                sector = 25;
            }
            else if (distance > drawService.projectionCoefficent * 170)
            {
                sector = 0;
            }
            else
            {
                var startRadSector = -2.9845105;
                var radSectorStep = 0.314159;
                var radSector = startRadSector;
                foreach (var proceedSector in sectors)
                {
                    if (angle >= radSector && angle < radSector + radSectorStep)
                    {
                        sector = proceedSector;
                        break;
                    }

                    sector = 11; // todo works, but not pretty

                    radSector += radSectorStep;
                }
            }

            return new Throw(poi, sector, multiplier, drawService.projectionFrameSide);
        }

        public void SaveRay(Ray ray)
        {
            rays.Add(ray);
        }
    }
}