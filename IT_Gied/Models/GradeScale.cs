namespace IT_Gied.Services
{
    public static class GradeScale
    {
       
        public static decimal ToPoints(int mark)
        {
            if (mark >= 95 && mark <= 100) return 4.2m;   // A+
            if (mark >= 85 && mark <= 94) return 4.0m;   // A
            if (mark >= 80 && mark <= 84) return 3.75m;  // A-
            if (mark >= 77 && mark <= 79) return 3.5m;   // B+
            if (mark >= 73 && mark <= 76) return 3.25m;  // B
            if (mark >= 70 && mark <= 72) return 3.0m;   // B-
            if (mark >= 67 && mark <= 69) return 2.75m;  // C+
            if (mark >= 63 && mark <= 66) return 2.5m;   // C
            if (mark >= 60 && mark <= 62) return 2.25m;  // C-
            if (mark >= 57 && mark <= 59) return 2.0m;   // D+
            if (mark >= 53 && mark <= 56) return 1.75m;  // D
            if (mark >= 50 && mark <= 52) return 1.5m;   // D-

        
            return 0.5m;//F
        }
    }
}
