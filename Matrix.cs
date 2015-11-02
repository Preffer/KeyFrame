using System;
using System.Diagnostics;

namespace Algebra {
    public class Matrix {
        private double[,] array;

        public Matrix(int row, int col) {
            array = new double[row, col];
        }

        public Matrix(double[,] init) {
            array = init;
        }

        public double this[int row, int col] {
            get {
                return array[row, col];
            }
            set {
                array[row, col] = value;
            }
        }

        public int Rows {
            get {
                return array.GetLength(0);
            }
        }

        public int Cols {
            get {
                return array.GetLength(1);
            }
        }

        public static Matrix operator* (Matrix one, Matrix two) {
            Debug.Assert(one.Cols == two.Rows);
            Debug.Assert(one.Rows == two.Cols);

            int rows = one.Rows;
            int cols = one.Cols;
            Matrix result = new Matrix(rows, rows);

            for (int row = 0; row < rows; row++) {
                for (int col = 0; col < rows; col++) {
                    double sum = 0;
                    for (int i = 0; i < cols; i++) {
                        sum += one[row, i] * two[i, row];
                    }
                    result[row, col] = sum;
                }
            }

            return result;
        }
    }
}
