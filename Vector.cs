using System;
using System.Diagnostics;

namespace Algebra {
    public class Vector {
        private double[] array;

        public Vector(int size) {
            array = new double[size];
        }

        public Vector(double[] init) {
            array = init;
        }

        public double this[int index] {
            get {
                return array[index];
            }
            set {
                array[index] = value;
            }
        }

        public int Length {
            get {
                return array.Length;
            }
        }

        public static double operator* (Vector one, Vector two) {
            Debug.Assert(one.Length == two.Length);

            double sum = 0;
            int length = one.Length;

            for (int i = 0; i < length; i++) {
                sum += one[i] * two[i];
            }

            return sum;
        }

        public static Vector operator* (Vector vector, Matrix matrix) {
            Debug.Assert(vector.Length == matrix.Rows);

            int rows = matrix.Rows;
            int cols = matrix.Cols;
            Vector result = new Vector(cols);

            for (int col = 0; col < cols; col++) {
                double sum = 0;
                for (int row = 0; row < rows; row++) {
                    sum += vector[row] * matrix[row, col];
                }
                result[col] = sum;
            }

            return result;
        }

        public static Vector operator* (Matrix matrix, Vector vector) {
            Debug.Assert(matrix.Cols == vector.Length);

            int rows = matrix.Rows;
            int cols = matrix.Cols;
            Vector result = new Vector(rows);

            for (int row = 0; row < rows; row++) {
                double sum = 0;
                for (int col = 0; col < cols; col++) {
                    sum += matrix[row, col] * vector[col];
                }
                result[row] = sum;
            }

            return result;
        }
    }
}
