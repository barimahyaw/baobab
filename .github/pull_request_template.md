# Pull Request

## 📋 **Summary**

<!-- Provide a brief description of the changes in this PR -->

### **What does this PR do?**
- [ ] 🐛 Bug fix
- [ ] ✨ New feature
- [ ] 🔄 Refactoring
- [ ] 📚 Documentation update
- [ ] 🧪 Tests
- [ ] 🔧 Build/CI changes
- [ ] 🎨 Code style changes

### **Description**
<!-- Describe your changes in detail -->

## 🔗 **Related Issues**

<!-- Link to related issues using # -->
Fixes #(issue number)
Relates to #(issue number)

## 🧪 **Testing**

### **Test Coverage**
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All tests pass locally

### **Test Plan**
<!-- Describe how you tested these changes -->

1. **Unit Tests:**
   - [ ] New tests for added functionality
   - [ ] Existing tests still pass
   
2. **Integration Tests:**
   - [ ] Database operations tested
   - [ ] API endpoints tested
   
3. **Manual Testing:**
   - [ ] Feature works as expected
   - [ ] No regression in existing functionality

## 📦 **Component Changes**

### **Domain Layer**
- [ ] New entities/value objects
- [ ] Domain events
- [ ] Business rules/validation
- [ ] Breaking changes to domain contracts

### **Application Layer**
- [ ] New commands/queries
- [ ] Pipeline behaviors
- [ ] Application services
- [ ] Breaking changes to application contracts

### **Infrastructure Layer**
- [ ] External service integrations
- [ ] Background jobs
- [ ] Caching changes
- [ ] Configuration changes

### **Persistence Layer**
- [ ] Database migrations
- [ ] Repository implementations
- [ ] Entity configurations
- [ ] Data seeding

### **Presentation Layer**
- [ ] New API endpoints
- [ ] Controller changes
- [ ] Response models
- [ ] API versioning

## ⚠️ **Breaking Changes**

<!-- List any breaking changes and migration steps -->

- [ ] No breaking changes
- [ ] Breaking changes (list below)

### **Breaking Changes Details:**
<!-- If there are breaking changes, describe them here -->

### **Migration Guide:**
<!-- Provide steps for users to migrate their code -->

## 🔒 **Security**

- [ ] No security implications
- [ ] Security review completed
- [ ] New security features added
- [ ] Security vulnerabilities addressed

### **Security Checklist:**
- [ ] No secrets in code
- [ ] Input validation added where needed
- [ ] Authorization checks in place
- [ ] Audit trails updated

## 📊 **Performance**

- [ ] No performance impact
- [ ] Performance improvements
- [ ] Performance regression (justified)

### **Performance Notes:**
<!-- Describe any performance considerations -->

## 📚 **Documentation**

- [ ] Code comments added/updated
- [ ] README updated
- [ ] API documentation updated
- [ ] Architecture documentation updated
- [ ] Examples updated

## ✅ **Checklist**

### **Code Quality**
- [ ] Code follows established patterns
- [ ] No code duplication
- [ ] Proper error handling
- [ ] Logging added where appropriate
- [ ] Code is well-commented

### **Standards Compliance**
- [ ] Follows Clean Architecture principles
- [ ] Implements DDD patterns correctly
- [ ] Uses Result pattern for error handling
- [ ] Follows CQRS pattern where applicable

### **Dependencies**
- [ ] No new dependencies added
- [ ] New dependencies justified and documented
- [ ] Dependency versions pinned appropriately

### **Build & CI**
- [ ] Build passes locally
- [ ] All automated tests pass
- [ ] Code coverage maintained or improved
- [ ] Static analysis passes

## 🖼️ **Screenshots**

<!-- Add screenshots if this PR affects the UI -->

## 📝 **Additional Notes**

<!-- Any additional information that reviewers should know -->

## 👥 **Reviewers**

<!-- Tag specific people for review if needed -->

/cc @username

---

### **For Maintainers:**

- [ ] Squash and merge
- [ ] Create merge commit
- [ ] Rebase and merge

**Labels to add:**
- Component: domain/application/infrastructure/persistence/presentation
- Type: bug/feature/docs/refactor/test
- Priority: low/medium/high/critical