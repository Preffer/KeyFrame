using System;
using System.Windows;

namespace KeyFrame {
    public class Polar {
        public double radius;
        public double angle;

        private static readonly Vector XAsis = new Vector(1, 0);

        public Polar(double radius, double angle) {
            this.radius = radius;
            this.angle = angle;
        }

        public Polar(Vector vector) {
            this.radius = vector.Length;
            this.angle = Vector.AngleBetween(vector, XAsis) * Math.PI / 180;
        }

        public Vector ToVector() {
            return new Vector(radius * Math.Cos(angle), -radius * Math.Sin(angle));
        }

        public static Polar operator +(Polar polar1, Polar polar2) {
            return new Polar(polar1.radius + polar2.radius, polar1.angle + polar2.angle);
        }

        public static Polar operator -(Polar polar1, Polar polar2) {
            return new Polar(polar1.radius - polar2.radius, polar1.angle - polar2.angle);
        }

        public static Polar operator *(Polar polar, double scalar) {
            return new Polar(polar.radius * scalar, polar.angle * scalar);
        }

        public static Polar operator /(Polar polar, double scalar) {
            return new Polar(polar.radius / scalar, polar.angle / scalar);
        }

        public static Polar Blend(Polar polar1, Polar polar2, double rate) {
            Polar result = polar1 * (1 - rate) + polar2 * rate;

            if (polar1.angle - polar2.angle > Math.PI) {
                result.angle = polar1.angle * (1 - rate) + (polar2.angle + 2 * Math.PI) * rate;
            } else {
                if (polar1.angle - polar2.angle < -Math.PI) {
                    result.angle = polar1.angle * (1 - rate) + (polar2.angle - 2 * Math.PI) * rate;
                }
            }

            return result;
        }
    }
}
