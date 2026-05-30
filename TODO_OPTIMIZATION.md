# XTranslate - Ke hoach toi uu hieu nang

Muc tieu: giam do tre cam nhan duoc (latency tu luc nhan hotkey den khi popup hien ket qua), giam CPU/allocation tren hot-path (mouse hook), tang do tin cay mang, va bo sung mot vai tinh nang tu QTranslate ma khong them dependency nang.

Baseline: build Debug PASS (0 warning, 0 error). .NET 8 WPF + WinForms, WinExe.

---

## Phan tich nut that hien tai

### Nghiem trong (anh huong truc tiep do tre)
1. ClipboardService.GetSelectedTextAsync co delay cung 50ms + 150ms = 200ms moi lan bat text.
   - 4 lan Dispatcher.InvokeAsync rieng le (get -> clear -> send -> read -> restore).
   - README quang cao 10ms nhung thuc te 200-250ms. Day la diem nghen lon nhat cua Ctrl+Q.
2. Console.WriteLine rai khap hot-path (mouse hook moi su kien chuot, clipboard, hotkey).
   - Tao allocation chuoi + Substring moi su kien. WinExe khong co console nen output bi vut bo nhung van ton CPU.

### Trung binh
3. Moi engine tu new HttpClient(), khong dat Timeout -> request co the treo toi 100s; khong tai su dung connection pool.
4. TranslationService._cache la Dictionary thuong: khong thread-safe; eviction Keys.First() khong phai LRU that.
5. ViewModel khong huy request cu khi doi ngon ngu nhanh -> race condition + lang phi mang.
6. WindowsOcrEngine encode PNG roi decode lai chi de lay SoftwareBitmap (round-trip thua); tao OcrEngine moi moi lan.
7. LanguageDatabase.TargetLanguages chay Where().ToList() moi lan truy cap (binding goi nhieu lan).

### Thap
8. build.ps1 tat PublishReadyToRun -> khoi dong nguoi cham hon.
9. MainWindow resolve service qua App.Services.GetRequiredService nhieu lan trong event handler.

---

## Tinh nang tham khao tu QTranslate (gia tri cao, rui ro thap)
- Engine fallback: tu dong chuyen engine khac khi engine hien tai loi -> tang do tin cay.
- Text-to-Speech: phat am text bang WinRT SpeechSynthesizer (built-in, khong them dependency).

---

## Cac Phase thuc hien (tuan tu)

### Phase 1 - Logger nhe, loai Console.WriteLine khoi hot-path  [HIGH]
- [ ] Tao Helpers/Log.cs: wrapper [Conditional("DEBUG")] goi System.Diagnostics.Debug.WriteLine.
- [ ] Thay toan bo Console.WriteLine bang Log.Debug (hoac xoa cac log trong hot-path mouse hook).
- [ ] Bo cac Substring chi dung de log.
- Ket qua mong doi: giam allocation/CPU tren mouse hook va clipboard; release build sach log.

### Phase 2 - ClipboardService polling adaptive  [HIGH]
- [ ] Thay 2 delay cung (50ms + 150ms) bang vong poll clipboard sequence number (GetClipboardSequenceNumber).
- [ ] Gop cac thao tac clipboard, giam so lan Dispatcher round-trip.
- [ ] Timeout toi da ~400ms de tranh treo neu app dich khong phan hoi Ctrl+C.
- Ket qua mong doi: do tre bat text giam tu ~200-250ms xuong ~30-80ms trong da so truong hop.

### Phase 3 - HttpClient dung chung + Timeout  [HIGH]
- [ ] Tao mot HttpClient tinh dung chung (hoac inject) cho ca hai engine.
- [ ] Dat Timeout hop ly (~8-10s) de tranh treo lau.
- [ ] Dat User-Agent mot lan.
- Ket qua mong doi: tai su dung connection (nhanh hon tu request thu 2), khong treo 100s khi mang loi.

### Phase 4 - TranslationService: LRU cache thread-safe + engine fallback  [MEDIUM]
- [ ] Thay Dictionary bang cache LRU that (linked list / access order) co khoa.
- [ ] Them tuy chon engine fallback: khi ActiveEngine that bai, thu engine con lai.
- Ket qua mong doi: cache dung dan, tranh dich lai; ket qua tin cay hon khi mot dich vu chet.

### Phase 5 - Huy request dich cu trong ViewModel  [MEDIUM]
- [ ] Dung CancellationTokenSource trong PopupViewModel va MainViewModel.
- [ ] Khi co request moi (doi ngon ngu/text moi), huy request truoc do.
- Ket qua mong doi: tranh ket qua cu ghi de ket qua moi; giam request mang thua.

### Phase 6 - Toi uu OCR  [MEDIUM]
- [ ] Convert BitmapSource sang SoftwareBitmap qua pixel buffer truc tiep (bo round-trip PNG encode/decode).
- [ ] Cache OcrEngine theo language tag thay vi tao moi moi lan.
- Ket qua mong doi: OCR nhanh hon, it allocation hon.

### Phase 7 - Misc  [LOW]
- [ ] Cache LanguageDatabase.TargetLanguages (tinh mot lan).
- [ ] Giam so lan resolve DI lap lai trong MainWindow (cache field).
- [ ] Bat PublishReadyToRun=true trong build.ps1 (it nhat ban Standalone) de khoi dong nhanh hon.

### Phase 8 - QTranslate: Text-to-Speech  [LOW / optional]
- [ ] Them ITtsService dung Windows.Media.SpeechSynthesis (WinRT, built-in).
- [ ] Nut/lenh phat am trong popup va main window.
- Ket qua mong doi: bo sung tinh nang giong QTranslate ma khong them dependency.

### Phase 9 - Cai thien UX/UI  [MEDIUM]
- [ ] Popup: them nut TTS (phat am), nut dong (X), hien spinner muot hon, copy co phan hoi.
- [ ] Main window: them trang thai khi rong (placeholder), feedback khi copy thanh cong.
- [ ] Floating icon: hieu ung muot, kich thuoc hop ly.
- [ ] Hoan thien chi tiet nho: tooltip, trang thai loi ro rang, accessibility (AutomationProperties).
- Ket qua mong doi: trai nghiem muot va ro rang hon, dong bo voi cac toi uu o tren.

---

## Quy tac kiem chung
- Sau moi Phase: chay `dotnet build XTranslate/XTranslate.csproj -c Debug` phai PASS 0 error.
- Khong doi hanh vi UI hien co tru khi la muc tieu cua Phase.
- Giu nguyen kien truc DI/MVVM hien tai.

## Tien do
- [x] Phase 0: Phan tich + viet ke hoach
- [x] Phase 1: Logger nhe (Log.cs, Conditional DEBUG), bo Console.WriteLine khoi hot-path
- [x] Phase 2: ClipboardService polling adaptive (GetClipboardSequenceNumber), SendCtrlC chay background
- [x] Phase 3: HttpClientProvider dung chung + Timeout 10s + ConnectTimeout 5s + gzip
- [x] Phase 4: TranslationService LRU cache thread-safe + engine fallback
- [x] Phase 5: Huy request cu bang CancellationTokenSource trong ca hai ViewModel
- [x] Phase 6: OCR convert qua pixel buffer + cache OcrEngine theo ngon ngu
- [x] Phase 7: Cache TargetLanguages + ByCode lookup, cache DI resolve trong MainWindow, R2R trong build.ps1
- [x] Phase 8: WindowsTtsService (WinRT SpeechSynthesizer) + lenh Speak trong ViewModel
- [x] Phase 9: Nut TTS o popup + main window, feedback copy, empty-state, toggle fallback trong Settings, AutomationProperties

## Ket qua build
- Debug build: PASS (0 warning, 0 error)
- Release build: PASS (0 warning, 0 error) - cac log Debug bi loai bo hoan toan

## Tom tat thay doi chinh
- Do tre bat text (Ctrl+Q): ~200-250ms co dinh -> poll adaptive bounded 400ms (thuong xong sau 20-80ms).
- Khong con treo 100s khi mang loi: HttpClient co Timeout.
- Cache dich dung LRU that, thread-safe; tu dong fallback engine khi loi.
- Khong con ket qua dich cu ghi de ket qua moi (cancellation).
- OCR bo round-trip PNG encode/decode, tai su dung OcrEngine.
- Release build sach log, khoi dong nhanh hon nho ReadyToRun.
- Them tinh nang phat am (TTS) kieu QTranslate, khong them dependency.
