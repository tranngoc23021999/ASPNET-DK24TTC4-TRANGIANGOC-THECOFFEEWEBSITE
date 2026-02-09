namespace CoffeSolution.Constants;

/// <summary>
/// Common messages used throughout the application.
/// Centralized for:
/// - Consistency in wording
/// - Easy localization/translation later
/// - Single source of truth
/// </summary>
public static class Messages
{
    // CRUD Success Messages
    public const string CreateSuccess = "Thêm mới thành công!";
    public const string UpdateSuccess = "Cập nhật thành công!";
    public const string DeleteSuccess = "Xóa thành công!";
    public const string SaveSuccess = "Lưu thành công!";

    // CRUD Error Messages
    public const string CreateError = "Thêm mới thất bại!";
    public const string UpdateError = "Cập nhật thất bại!";
    public const string DeleteError = "Xóa thất bại!";
    public const string SaveError = "Lưu thất bại!";

    // Not Found Messages
    public const string NotFound = "Không tìm thấy dữ liệu!";
    public const string StoreNotFound = "Không tìm thấy cửa hàng!";
    public const string ProductNotFound = "Không tìm thấy sản phẩm!";
    public const string OrderNotFound = "Không tìm thấy đơn hàng!";
    public const string SupplierNotFound = "Không tìm thấy nhà cung cấp!";
    public const string UserNotFound = "Không tìm thấy người dùng!";
    public const string CustomerNotFound = "Không tìm thấy khách hàng!";

    // Validation Messages
    public const string InvalidData = "Dữ liệu không hợp lệ!";
    public const string RequiredField = "Vui lòng nhập đầy đủ thông tin!";
    public const string DuplicateData = "Dữ liệu đã tồn tại!";
    public const string InvalidId = "ID không hợp lệ!";

    // Permission Messages
    public const string AccessDenied = "Bạn không có quyền truy cập!";
    public const string Unauthorized = "Vui lòng đăng nhập để tiếp tục!";

    // Auth Messages
    public const string LoginSuccess = "Đăng nhập thành công!";
    public const string LoginFailed = "Tên đăng nhập hoặc mật khẩu không đúng!";
    public const string LogoutSuccess = "Đăng xuất thành công!";
    public const string PasswordChanged = "Đổi mật khẩu thành công!";
    public const string PasswordChangeFailed = "Đổi mật khẩu thất bại!";
    public const string InvalidPassword = "Mật khẩu cũ không đúng!";

    // Confirmation Messages
    public const string ConfirmDelete = "Bạn có chắc chắn muốn xóa?";
    public const string ConfirmCancel = "Bạn có chắc chắn muốn hủy?";

    // Status Messages
    public const string StatusActive = "Đang hoạt động";
    public const string StatusInactive = "Ngừng hoạt động";
    public const string StatusPending = "Đang chờ xử lý";
    public const string StatusCompleted = "Đã hoàn thành";
    public const string StatusCancelled = "Đã hủy";

    // Order Messages
    public const string OrderCreated = "Tạo đơn hàng thành công!";
    public const string OrderCompleted = "Đơn hàng đã hoàn thành!";
    public const string OrderCancelled = "Đơn hàng đã bị hủy!";
    public const string PaymentSuccess = "Thanh toán thành công!";
    public const string PaymentFailed = "Thanh toán thất bại!";

    // Warehouse Messages
    public const string ReceiptCreated = "Tạo phiếu nhập kho thành công!";
    public const string ReceiptApproved = "Duyệt phiếu nhập kho thành công!";
    public const string InsufficientStock = "Số lượng tồn kho không đủ!";

    // Generic Messages
    public const string OperationSuccess = "Thao tác thành công!";
    public const string OperationFailed = "Thao tác thất bại!";
    public const string UnexpectedError = "Đã xảy ra lỗi không mong muốn!";
    public const string TryAgainLater = "Vui lòng thử lại sau!";
}
