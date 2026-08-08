/** The allocation of a teacher to a class and subject pair, with names resolved. */
export interface TeacherAssignment {
  id: string;
  teacherId: string;
  teacherName: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
}

export interface CreateTeacherAssignmentRequest {
  teacherId: string;
  classId: string;
  subjectId: string;
}
