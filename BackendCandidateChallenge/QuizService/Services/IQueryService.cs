using QuizService.Model;
using System.Collections.Generic;

namespace QuizService.Services
{
    public interface IQueryService
    {
        public IEnumerable<QuizResponseModel> FetchQuizInfo();
        public object FetchQuizById(int id);
        public object CreateQuiz(QuizCreateModel value);
        public int UpdateQuizModel(int id, QuizUpdateModel value);
        public int DeleteQuiz(int id);
        public int CreateNewQuestionsForQuiz(int id, QuestionCreateModel value);
        public int UpdateQuestion(int qid, QuestionUpdateModel value);
        public void DeleteQuestionById(int qid);
        public int CreateAnswer(int qid, AnswerCreateModel value);
        public int UpdateAnswer(int qid, AnswerUpdateModel value);
        public void DeleteAnswer(int aid);
    }
}
