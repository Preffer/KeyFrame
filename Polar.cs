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
            radius = vector.Length;
            angle = Vector.AngleBetween(vector, XAsis) * Math.PI / 180;

            if (angle < 0) angle += 2 * Math.PI;
        }

        public Vector ToVector() {
            return new Vector(radius * Math.Cos(angle), -radius * Math.Sin(angle));
        }

        public static Polar operator -(Polar polar1, Polar polar2) {
            return new Polar(polar1.radius - polar2.radius, polar1.angle - polar2.angle);
        }

        public static Polar operator +(Polar polar1, Polar polar2) {
            return new Polar(polar1.radius + polar2.radius, polar1.angle + polar2.angle);
        }

        public static Polar operator *(Polar polar, double scalar) {
            return new Polar(polar.radius * scalar, polar.angle * scalar);
        }

        public static Polar operator /(Polar polar, double scalar) {
            return new Polar(polar.radius / scalar, polar.angle / scalar);
        }

    }
}
