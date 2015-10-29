using System;
using System.Collections.Generic;
using MVCForum.Domain.DomainModel;
using MVCForum.Domain.Interfaces.UnitOfWork;

namespace MVCForum.Domain.Interfaces.Services
{
    public enum LoginAttemptStatus
    {
        LoginSuccessful,
        UserNotFound,
        PasswordIncorrect,
        PasswordAttemptsExceeded,
        UserLockedOut,
        UserNotApproved,
        Banned
    }

    public partial interface IMembershipService
    {
        MembershipUser SanitizeUser(MembershipUser membershipUser);
        string[] GetRolesForUser(Guid userId);
        MembershipUser GetUserById(Guid? UserId, bool removeTracking = false);
        MembershipUser GetUserByEmail(string email);
        MembershipUser GetUserBySlug(string slug);
        IList<MembershipUser> GetUsersById(List<Guid> guids);
        IList<MembershipUser> GetUsersByDaysPostsPoints(int amoutOfDaysSinceRegistered, int amoutOfPosts);
        MembershipUser GetUser(Guid id);
        MembershipCreateStatus CreateUser(MembershipUser newUser);
        string ErrorCodeToString(MembershipCreateStatus createStatus);
        MembershipUser CreateEmptyUser();
        IList<MembershipUser> GetAll();
        PagedList<MembershipUser> GetAll(int pageIndex, int pageSize);
        PagedList<MembershipUser> SearchMembers(string search, int pageIndex, int pageSize);
        IList<MembershipUser> SearchMembers(string username, int amount);
        IList<MembershipUser> GetActiveMembers();
        void ProfileUpdated(MembershipUser user);
        bool Delete(MembershipUser user, IUnitOfWork unitOfWork);
        IList<MembershipUser> GetLatestUsers(int amountToTake);
        IList<MembershipUser> GetLowestPointUsers(int amountToTake);
        int MemberCount();
        string ToCsv();
        CsvReport FromCsv(List<string> allLines);

        /// <summary>
        /// Completed scrubs a users account clean
        /// Clears everything - Posts, polls, votes, favourites, profile etc...
        /// </summary>
        /// <param name="user"></param>
        /// <param name="unitOfWork"></param>
        void ScrubUsers(MembershipUser user, IUnitOfWork unitOfWork);
    }
}
