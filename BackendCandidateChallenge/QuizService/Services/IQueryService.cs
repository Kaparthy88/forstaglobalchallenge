using QuizService.Model;
using QuizService.Model.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizService.Services
{
    public interface IQueryService
    {
        public IEnumerable<QuizResponseModel> FetchQuizInfo();
        public Task<object> FetchQuizById(int id);
        public Task<IEnumerable<Question>> FetchQuestionByQuizId(int id);
        public QuizResponseModel CreateQuizResponseModel(int id, Quiz quiz, IEnumerable<Question> questions, Dictionary<int, IList<Answer>> answers);
        public object CreateQuiz(QuizCreateModel value);
        public int UpdateQuizModel(int id, QuizUpdateModel value);
        public int DeleteQuiz(int id);
        public object CreateNewQuestionsForQuiz(int id, QuestionCreateModel value);
        public int UpdateQuestion(int qid, QuestionUpdateModel value);
        public void DeleteQuestionById(int qid);
        public object CreateAnswer(int qid, AnswerCreateModel value);
        public int UpdateAnswer(int qid, AnswerUpdateModel value);
        public void DeleteAnswer(int aid);
    }
}
