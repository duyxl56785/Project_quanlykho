// JS dùng chung cho cả hệ thống: hộp xác nhận, ô quét mã, gọi API, phiếu có dòng hàng.
const QuanLyKho = (() => {
    // ---------- Hộp xác nhận: data-xac-nhan="Câu hỏi?" trên form, nút hoặc liên kết ----------
    const hop = document.getElementById("hopXacNhan");
    let hanhDongCho = null;

    function hoi(cauHoi, khiDongY) {
        hop.querySelector("[data-noi-dung]").textContent = cauHoi;
        hanhDongCho = khiDongY;
        bootstrap.Modal.getOrCreateInstance(hop).show();
    }

    hop?.querySelector("[data-dong-y]").addEventListener("click", () => {
        bootstrap.Modal.getOrCreateInstance(hop).hide();
        const lam = hanhDongCho;
        hanhDongCho = null;
        lam?.();
    });

    document.addEventListener("submit", (e) => {
        const form = e.target;
        const cauHoi = form.dataset.xacNhan ?? e.submitter?.dataset.xacNhan;
        if (!cauHoi || form.dataset.daXacNhan) return;
        e.preventDefault();
        hoi(cauHoi, () => {
            form.dataset.daXacNhan = "1";
            form.requestSubmit(e.submitter ?? undefined);
        });
    });

    // Liên kết: đồng ý thì đi tới href. Nút type="button": đồng ý thì phát sự kiện "da-xac-nhan" trên nút.
    document.addEventListener("click", (e) => {
        const nguon = e.target.closest('a[data-xac-nhan], button[type="button"][data-xac-nhan]');
        if (!nguon) return;
        e.preventDefault();
        hoi(nguon.dataset.xacNhan, () => nguon.tagName === "A"
            ? window.location.assign(nguon.href)
            : nguon.dispatchEvent(new CustomEvent("da-xac-nhan", { bubbles: true })));
    });

    // ---------- Ô quét mã: Enter phát sự kiện "quet-ma" ----------
    document.addEventListener("keydown", (e) => {
        const o = e.target;
        if (e.key !== "Enter" || !o.matches?.("[data-quet-ma]")) return;
        e.preventDefault();
        const ma = o.value.trim();
        o.value = "";
        if (ma) o.dispatchEvent(new CustomEvent("quet-ma", { detail: ma, bubbles: true }));
        // Màn hình đã chuyển con trỏ sang ô số lượng thì giữ nguyên.
        if (document.activeElement === o || document.activeElement === document.body) o.focus();
    });

    let amThanh = null;
    function bip(tanSo) {
        try {
            amThanh ??= new AudioContext();
            const dao = amThanh.createOscillator();
            dao.frequency.value = tanSo;
            dao.connect(amThanh.destination);
            dao.start();
            dao.stop(amThanh.currentTime + 0.12);
        } catch { /* trình duyệt chặn âm thanh thì bỏ qua */ }
    }

    /** Báo kết quả quét ngay dưới ô: màu, chữ và tiếng bíp (thấp khi lỗi). */
    function baoQuet(o, thongDiep, thanhCong) {
        const khung = o.closest(".o-quet-ma");
        khung.classList.toggle("thanh-cong", thanhCong);
        khung.classList.toggle("dang-loi", !thanhCong);
        khung.querySelector(".thong-bao-quet").textContent = thongDiep;
        bip(thanhCong ? 880 : 220);
        if (document.activeElement === document.body) o.focus();
    }

    // ---------- Gọi API ----------
    /**
     * Gọi API JSON. Request ghi (khác GET) tự gửi Idempotency-Key (truyền khoaChongLap để bấm lặp dùng lại cùng khóa)
     * và token chống CSRF. Trả { ok, status, duLieu, loi } với loi lấy từ JSON { loi } của máy chủ.
     */
    async function goiApi(url, { method = "GET", body, khoaChongLap } = {}) {
        const headers = { "Accept": "application/json" };
        if (method !== "GET") {
            headers["Idempotency-Key"] = khoaChongLap ?? crypto.randomUUID();
            headers["RequestVerificationToken"] = document.querySelector('meta[name="xsrf-token"]')?.content ?? "";
        }
        if (body !== undefined) headers["Content-Type"] = "application/json";
        try {
            const res = await fetch(url, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
            const duLieu = res.headers.get("content-type")?.includes("json") ? await res.json() : null;
            const loi = res.ok ? null
                : duLieu?.loi ?? (res.status === 401 ? "Phiên đăng nhập đã hết, vui lòng đăng nhập lại."
                    : res.status === 403 ? "Bạn không có quyền thực hiện thao tác này."
                    : `Lỗi máy chủ (${res.status}).`);
            return { ok: res.ok, status: res.status, duLieu, loi };
        } catch {
            return { ok: false, status: 0, duLieu: null, loi: "Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại." };
        }
    }

    // ---------- Số việc chờ trên menu: <span data-dem="/api/..."> ----------
    document.querySelectorAll("[data-dem]").forEach(async (o) => {
        const kq = await goiApi(o.dataset.dem);
        if (kq.ok && kq.duLieu?.so > 0) {
            o.textContent = kq.duLieu.so;
            o.setAttribute("aria-label", `${kq.duLieu.so} việc đang chờ`);
            o.hidden = false;
        }
    });

    // ---------- Nội dung nạp sau: <div data-tai="/url"> nhận HTML; nút data-tai-lai nạp lại phần trong cùng .the, data-tai-lai-tat-ca nạp lại hết ----------
    const mauDangTai = '<div class="dang-tai" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span>Đang tải…</span></div>';
    async function taiNoiDung(vung) {
        vung.innerHTML = mauDangTai;
        try {
            const r = await fetch(vung.dataset.tai, { headers: { Accept: "text/html" } });
            // Hết phiên thì máy chủ chuyển sang trang đăng nhập: tải lại cả trang thay vì nhét trang đó vào thẻ.
            if (r.redirected) return window.location.reload();
            if (!r.ok) throw new Error();
            vung.innerHTML = await r.text();
        } catch {
            vung.innerHTML = '<div class="alert alert-danger mb-0" role="alert">Không tải được số liệu. Bấm Làm mới để thử lại.</div>';
        }
    }
    document.querySelectorAll("[data-tai]").forEach(taiNoiDung);
    document.addEventListener("click", (e) => {
        if (e.target.closest("[data-tai-lai-tat-ca]")) document.querySelectorAll("[data-tai]").forEach(taiNoiDung);
        const nut = e.target.closest("[data-tai-lai]");
        if (nut) nut.closest(".the").querySelectorAll("[data-tai]").forEach(taiNoiDung);
    });

    // ---------- Ô chọn đơn vị theo hàng: <script type="application/json" data-don-vi-hang> { maBienThe: [{ ten, heSo }] };
    // trong mỗi dòng select[data-chon-hang] và select[data-don-vi] (partial _OChonDonVi); đổi hàng thì nạp lại đơn vị ----------
    const donViHang = JSON.parse(document.querySelector("[data-don-vi-hang]")?.textContent || "{}");
    function napDonVi(dong) {
        const chon = dong.querySelector("[data-don-vi]");
        if (!chon) return;
        const ds = donViHang[dong.querySelector("[data-chon-hang]")?.value] ?? [];
        const cu = chon.value;
        chon.replaceChildren(...ds.map((d) => {
            const o = new Option(d.heSo === 1 ? d.ten : `${d.ten} (${d.heSo} ${ds[0].ten})`, d.ten, false, d.ten === cu);
            o.dataset.heSo = d.heSo;
            return o;
        }));
    }
    document.addEventListener("change", (e) => {
        const hang = e.target.closest("[data-chon-hang]");
        if (hang) napDonVi(hang.closest("tr"));
    });

    // ---------- Phiếu có dòng hàng ----------
    // <tbody data-dong-hang> + <template> chứa một dòng với tên "Dong[__i__].X"; nút data-them-dong="#idTbody", nút data-xoa-dong trong dòng.
    function danhSoLai(tbody) {
        tbody.querySelectorAll("tr").forEach((tr, i) => {
            tr.querySelectorAll("[name]").forEach((o) => { o.name = o.name.replace(/\[\d+\]/, `[${i}]`); });
            tr.querySelector("[data-stt]")?.replaceChildren(String(i + 1));
        });
        tbody.dispatchEvent(new Event("dong-hang-doi", { bubbles: true }));
    }

    document.addEventListener("click", (e) => {
        const nutThem = e.target.closest("[data-them-dong]");
        if (nutThem) {
            const tbody = document.querySelector(nutThem.dataset.themDong);
            const mau = tbody.closest("table").querySelector("template");
            const dong = mau.content.firstElementChild.cloneNode(true);
            dong.querySelectorAll("[name]").forEach((o) => { o.name = o.name.replace("__i__", "0"); });
            tbody.append(dong);
            danhSoLai(tbody);
            dong.querySelector("input, select")?.focus();
            return;
        }
        const nutXoa = e.target.closest("[data-xoa-dong]");
        if (nutXoa) {
            const tbody = nutXoa.closest("tbody");
            nutXoa.closest("tr").remove();
            danhSoLai(tbody);
        }
    });

    const dinhDangSo = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 3 });
    const dinhDangTien = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" });

    return { hoi, baoQuet, goiApi, dinhDangSo, dinhDangTien };
})();
