document.addEventListener('DOMContentLoaded', () => {
    const apiUrl = 'http://localhost:5190/api/v1'; // API'nizin çalıştığı portu KONTROL EDİN!
    let jwtToken = localStorage.getItem('jwtToken');
    let userRole = localStorage.getItem('userRole');

    // Auth Section Elements
    const authSection = document.getElementById('auth-section');
    const registerForm = document.getElementById('register-form');
    const loginForm = document.getElementById('login-form');
    const regUsernameInput = document.getElementById('reg-username');
    const regPasswordInput = document.getElementById('reg-password');
    const loginUsernameInput = document.getElementById('login-username');
    const loginPasswordInput = document.getElementById('login-password');
    const registerMessage = document.getElementById('register-message');
    const loginMessage = document.getElementById('login-message');
    const logoutButton = document.getElementById('logout-button');
    console.log("Sayfa yüklendi, logoutButton elementi:", logoutButton); // LOG: Buton bulundu mu?

    // Stocks Section Elements
    const stocksSection = document.getElementById('stocks-section');
    const getAllStocksButton = document.getElementById('get-all-stocks-button');
    const tickerSymbolInput = document.getElementById('ticker-symbol-input');
    const getSingleStockButton = document.getElementById('get-single-stock-button');
    const getHtmlReportButton = document.getElementById('get-html-report-button');
    const stocksOutput = document.getElementById('stocks-output');
    const reportOutput = document.getElementById('report-output');

    // Admin Panel Elements
    const adminPanelSection = document.getElementById('admin-panel-section');
    const trackSymbolForm = document.getElementById('track-symbol-form');
    const newTickerSymbolInput = document.getElementById('new-ticker-symbol');
    const trackMessage = document.getElementById('track-message');
    const getTrackedSymbolsButton = document.getElementById('get-tracked-symbols-button');
    const trackedSymbolsList = document.getElementById('tracked-symbols-list');
    const untrackMessage = document.getElementById('untrack-message');

    // === YENİ: Kullanıcı Tercihleri Elementleri ===
    const stockPreferenceSection = document.getElementById('stock-preference-section');
    const prefTickerSymbolDisplay = document.getElementById('pref-ticker-symbol');
    const preferenceForm = document.getElementById('preference-form');
    const prefHiddenTickerInput = document.getElementById('pref-hidden-ticker');
    const prefNotesInput = document.getElementById('pref-notes');
    const prefAlertUpperInput = document.getElementById('pref-alert-upper');
    const prefAlertLowerInput = document.getElementById('pref-alert-lower');
    const deletePreferenceButton = document.getElementById('delete-preference-button');
    const preferenceMessage = document.getElementById('preference-message');
// app.js'in en üstüne modal elementlerini ekle (eğer yoksa)
const chartModal = document.getElementById('chart-modal');
const chartModalTitle = document.getElementById('chart-modal-title');
const chartContainer = document.getElementById('tradingview-chart-widget'); // DİKKAT: ID değişti
const closeModalButton = document.querySelector('.close-button');

// Kapatma butonları için olay dinleyicileri (eğer yoksa)
if(closeModalButton) {
    closeModalButton.addEventListener('click', () => {
        if (chartModal) chartModal.style.display = 'none';
    });
}
window.addEventListener('click', (event) => {
    if (event.target == chartModal) {
        if (chartModal) chartModal.style.display = 'none';
    }
});


    // YENİ showStockChart FONKSİYONU
    function showStockChart(ticker) {
        if (!ticker) return;

        // Modal başlığını ayarla
        chartModalTitle.textContent = `${ticker} Grafiği`;
        
        // Önceki widget'ı temizle (varsa)
        if (chartContainer) {
            chartContainer.innerHTML = '';
        } else {
            console.error("chart-container elementi bulunamadı!");
            return;
        }

        // TradingView widget'ını dinamik olarak oluştur
       const widgetConfig = {
        "lineWidth": 2,
        "lineType": 0,
        "chartType": "area",
        "fontColor": "rgb(106, 109, 120)",
        "gridLineColor": "rgba(242, 242, 242, 0.06)",
        "volumeUpColor": "rgba(34, 171, 148, 0.5)",
        "volumeDownColor": "rgba(247, 82, 95, 0.5)",
        "backgroundColor": "#0F0F0F",
        "widgetFontColor": "#DBDBDB",
        "upColor": "#22ab94",
        "downColor": "#f7525f",
        "borderUpColor": "#22ab94",
        "borderDownColor": "#f7525f",
        "wickUpColor": "#22ab94",
        "wickDownColor": "#f7525f",
        "colorTheme": "dark",
        "isTransparent": false,
        "locale": "tr", // Türkçe için 'tr' olarak değiştirdim
        "chartOnly": false,
        "scalePosition": "right",
        "scaleMode": "Normal",
        "fontFamily": "-apple-system, BlinkMacSystemFont, Trebuchet MS, Roboto, Ubuntu, sans-serif",
        "valuesTracking": "1",
        "changeMode": "price-and-percent",
        
        // --- DİNAMİK KISIM ---
        // 'symbols' dizisi sadece tıklanan hisse senedi ile dolduruluyor.
        "symbols": [
            [
                ticker, // Görünen isim
                `${ticker}|1D` // TradingView sembol formatı
            ]
        ],
        
        "dateRanges": [
            "1d|1",
            "1m|30",
            "3m|60",
            "12m|1D",
            "60m|1W",
            "all|1M"
        ],
        "fontSize": "10",
        "headerFontSize": "medium",
        
        // --- BOYUTLANDIRMA DEĞİŞİKLİKLERİ ---
        "autosize": false,
        "width": 960
        ,
        "height": 500,
        // ------------------------------------

        "noTimeScale": false,
        "hideDateRanges": false,
        "hideMarketStatus": false,
        "hideSymbolLogo": false,
        "container_id": "tradingview-chart-widget" // Widget'ın yerleşeceği div'in ID'si
    };

        // Yeni bir script elementi oluştur
        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = 'https://s3.tradingview.com/external-embedding/embed-widget-symbol-overview.js';
        script.async = true;
        script.innerHTML = JSON.stringify(widgetConfig);

        // Script'i widget container'ına ekle
        chartContainer.appendChild(script);

        // Modalı göster
        if (chartModal) chartModal.style.display = 'flex';
    }
    // --- Yardımcı Fonksiyonlar ---
    function displayMessage(element, message, isSuccess) {
        if (!element) return;
        element.textContent = message;
        element.className = isSuccess ? 'message success' : 'message error';
    }

    function escapeHtml(unsafe) {
        if (unsafe === null || typeof unsafe === 'undefined' || typeof unsafe !== 'string') return '';
        return unsafe
            .replace(/&/g, "&") // Corrected & to &
            .replace(/</g, "<")  // Corrected < to <
            .replace(/>/g, ">")  // Corrected > to >
            .replace(/"/g, "&quot;")    
            .replace(/'/g, "'"); // Corrected ' to '
    }

    function updateUIBasedOnAuthState() {
        jwtToken = localStorage.getItem('jwtToken');
        userRole = localStorage.getItem('userRole');
        console.log("[updateUI] Çağrıldı. Token:", jwtToken, "Role:", userRole);

        if (jwtToken) { // Kullanıcı giriş yapmışsa
            if (authSection) authSection.style.display = 'none';
            if (stocksSection) stocksSection.style.display = 'block';

            if (logoutButton) {
                logoutButton.style.display = 'inline-block';
                console.log("[updateUI] Çıkış Yap butonu gösterilmeye çalışılıyor. Gerçek display stili:", window.getComputedStyle(logoutButton).display);
            } else {
                console.warn("[updateUI] Çıkış Yap butonu (logout-button) HTML'de bulunamadı!");
            }

            if (adminPanelSection) {
                if (userRole === 'Admin') {
                    adminPanelSection.style.display = 'block';
                    console.log("[updateUI] Admin paneli gösteriliyor.");
                } else {
                    adminPanelSection.style.display = 'none';
                    console.log("[updateUI] Admin paneli gizleniyor (rol Admin değil veya rol yok).");
                }
            } else if (userRole === 'Admin') { // Admin rolü var ama panel elementi yoksa uyar
                 console.warn("[updateUI] Admin rolü var ama Admin panel section (admin-panel-section) HTML'de bulunamadı!");
            }
             // Hide preference section by default, only show when editing
            if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';

        } else { // Kullanıcı çıkış yapmışsa
            if (authSection) authSection.style.display = 'block';
            if (stocksSection) stocksSection.style.display = 'none';

            if (logoutButton) {
                logoutButton.style.display = 'none';
                console.log("[updateUI] Çıkış Yap butonu gizlenmeye çalışılıyor.");
            } else {
                console.warn("[updateUI] Çıkış Yap butonu (logout-button) HTML'de bulunamadı (çıkış durumu)!");
            }

            if (adminPanelSection) {
                adminPanelSection.style.display = 'none';
                console.log("[updateUI] Çıkış yapıldı, admin paneli gizleniyor.");
            }
            // Hide preference section on logout
            if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';


            if (stocksOutput) stocksOutput.innerHTML = '';
            if (reportOutput) { reportOutput.innerHTML = ''; reportOutput.style.display = 'none'; }
            if (trackedSymbolsList) trackedSymbolsList.innerHTML = '';
        }
    }

    // --- Olay Dinleyicileri ---

    if (registerForm) {
        registerForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            const username = regUsernameInput.value;
            const password = regPasswordInput.value;
            displayMessage(registerMessage, 'Kayıt yapılıyor...', true);
            try {
                const response = await fetch(`${apiUrl}/auth/register`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });
                const responseData = await response.json();
                if (response.ok) {
                    displayMessage(registerMessage, responseData.message || 'Kayıt başarılı!', true);
                    regUsernameInput.value = '';
                    regPasswordInput.value = '';
                } else {
                    displayMessage(registerMessage, responseData.message || `Hata: ${response.statusText}`, false);
                }
            } catch (error) {
                console.error('Register error:', error);
                displayMessage(registerMessage, 'Kayıt sırasında bir ağ hatası oluştu.', false);
            }
        });
    }

    if (loginForm) {
        loginForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            const username = loginUsernameInput.value;
            const password = loginPasswordInput.value;
            displayMessage(loginMessage, 'Giriş yapılıyor...', true);
            try {
                const response = await fetch(`${apiUrl}/auth/login`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password })
                });
                const responseData = await response.json();
                console.log("API'den Gelen Login Yanıtı:", responseData); // LOG 7

                if (response.ok && responseData.token) {
                    jwtToken = responseData.token;
                    userRole = responseData.role; // API'den gelen rolü al
                    localStorage.setItem('jwtToken', jwtToken);
                    localStorage.setItem('userRole', userRole); // Rolü localStorage'a kaydet
                    console.log("Login sonrası atanan userRole:", userRole); // LOG 8

                    displayMessage(loginMessage, `Giriş başarılı! Hoş geldin ${escapeHtml(responseData.username)}. Rol: ${escapeHtml(userRole)}`, true);
                    loginUsernameInput.value = '';
                    loginPasswordInput.value = '';
                    updateUIBasedOnAuthState();
                } else {
                    jwtToken = null; userRole = null;
                    localStorage.removeItem('jwtToken'); localStorage.removeItem('userRole');
                    displayMessage(loginMessage, responseData.message || `Hata: ${response.statusText}`, false);
                    updateUIBasedOnAuthState();
                }
            } catch (error) {
                console.error('Login error:', error);
                jwtToken = null; userRole = null;
                localStorage.removeItem('jwtToken'); localStorage.removeItem('userRole');
                displayMessage(loginMessage, 'Giriş sırasında bir ağ hatası oluştu.', false);
                updateUIBasedOnAuthState();
            }
        });
    }

   if (logoutButton) {
    logoutButton.addEventListener('click', () => {
        jwtToken = null;
        userRole = null;
        localStorage.removeItem('jwtToken');
        localStorage.removeItem('userRole');
        updateUIBasedOnAuthState();
        displayMessage(loginMessage, 'Başarıyla çıkış yapıldı.', true);
        setTimeout(() => { if(loginMessage) displayMessage(loginMessage, '', true); }, 3000);
        // Hide preferences form and clear its fields on logout
        if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';
        if (prefNotesInput) prefNotesInput.value = '';
        if (prefAlertUpperInput) prefAlertUpperInput.value = '';
        if (prefAlertLowerInput) prefAlertLowerInput.value = '';
        if (prefTickerSymbolDisplay) prefTickerSymbolDisplay.textContent = '';
        if (prefHiddenTickerInput) prefHiddenTickerInput.value = '';
        if (preferenceMessage) displayMessage(preferenceMessage, '', true);
    });
}

    if (getAllStocksButton) {
        getAllStocksButton.addEventListener('click', async () => {
            if (!jwtToken) { alert('Lütfen önce giriş yapın.'); return; }
            if(stocksOutput) stocksOutput.innerHTML = 'Yükleniyor...';
            if(reportOutput) { reportOutput.style.display = 'none'; reportOutput.innerHTML = '';}
             // Hide preference section when fetching all stocks
            if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';
            try {
                const response = await fetch(`${apiUrl}/stocks`, {
                    method: 'GET',
                    headers: { 'Authorization': `Bearer ${jwtToken}`, 'Accept': 'application/xml' }
                });
                if (response.ok) {
                    const xmlText = await response.text();
                    parseAndDisplayXmlAsTable(xmlText, stocksOutput);
                } else if (response.status === 401) {
                    if(stocksOutput) stocksOutput.innerHTML = '<p class="message error">Yetkisiz erişim. Lütfen tekrar giriş yapın.</p>';
                    jwtToken = null; userRole = null; localStorage.removeItem('jwtToken'); localStorage.removeItem('userRole'); updateUIBasedOnAuthState();
                } else {
                    const errorText = await response.text();
                    if(stocksOutput) stocksOutput.innerHTML = `<p class="message error">Hata: ${response.status} - ${escapeHtml(errorText) || response.statusText}</p>`;
                }
            } catch (error) {
                console.error('Get All Stocks error:', error);
                if(stocksOutput) stocksOutput.innerHTML = '<p class="message error">Hisseleri getirirken bir ağ hatası oluştu.</p>';
            }
        });
    }

    if (getSingleStockButton) {
        getSingleStockButton.addEventListener('click', async () => {
            if (!jwtToken) { alert('Lütfen önce giriş yapın.'); return; }
            const ticker = tickerSymbolInput.value.trim().toUpperCase();
            if (!ticker) { alert('Lütfen bir hisse sembolü girin.'); return; }
            if(stocksOutput) stocksOutput.innerHTML = 'Yükleniyor...';
            if(reportOutput) { reportOutput.style.display = 'none'; reportOutput.innerHTML = '';}
            // Hide preference section when fetching single stock initially
            if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';
            try {
                const response = await fetch(`${apiUrl}/stocks/${ticker}`, {
                    method: 'GET',
                    headers: { 'Authorization': `Bearer ${jwtToken}`, 'Accept': 'application/xml' }
                });
                if (response.ok) {
                    const xmlText = await response.text();
                    parseAndDisplayXmlAsTable(xmlText, stocksOutput, true);
                } else if (response.status === 401) {
                    if(stocksOutput) stocksOutput.innerHTML = '<p class="message error">Yetkisiz erişim. Lütfen tekrar giriş yapın.</p>';
                    jwtToken = null; userRole = null; localStorage.removeItem('jwtToken'); localStorage.removeItem('userRole'); updateUIBasedOnAuthState();
                } else if (response.status === 404) {
                    if(stocksOutput) stocksOutput.innerHTML = `<p class="message error">'${escapeHtml(ticker)}' sembolüne sahip hisse senedi bulunamadı.</p>`;
                } else {
                    const errorText = await response.text();
                    if(stocksOutput) stocksOutput.innerHTML = `<p class="message error">Hata: ${response.status} - ${escapeHtml(errorText) || response.statusText}</p>`;
                }
            } catch (error) {
                console.error('Get Single Stock error:', error);
                if(stocksOutput) stocksOutput.innerHTML = '<p class="message error">Hisseyi getirirken bir ağ hatası oluştu.</p>';
            }
        });
    }

    if (getHtmlReportButton) {
        getHtmlReportButton.addEventListener('click', async () => {
            if (stocksOutput) stocksOutput.innerHTML = '';
            if (reportOutput) {
                 reportOutput.innerHTML = 'HTML Raporu Yükleniyor...';
                 reportOutput.style.display = 'block';
            }
            // Hide preference section when showing HTML report
            if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';
            try {
                const response = await fetch(`${apiUrl}/stocks/report/html`, {
                    method: 'GET', headers: { 'Accept': 'text/html' }
                });
                if (response.ok) {
                    const htmlText = await response.text();
                    if (reportOutput) reportOutput.innerHTML = `<iframe srcdoc="${escapeHtml(htmlText)}" style="width:100%; height: 600px; border:1px solid #ccc;"></iframe>`;
                } else {
                    const errorText = await response.text();
                    if (reportOutput) reportOutput.innerHTML = `<p class="message error">HTML Raporu alınırken hata: ${response.status} - ${escapeHtml(errorText) || response.statusText}</p>`;
                }
            } catch (error) {
                console.error('Get HTML Report error:', error);
                if (reportOutput) reportOutput.innerHTML = '<p class="message error">HTML Raporu alınırken bir ağ hatası oluştu.</p>';
            }
        });
    }

    // Admin Paneli Olay Dinleyicileri
    if (trackSymbolForm) {
        trackSymbolForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            if (!jwtToken || userRole !== 'Admin') { displayMessage(trackMessage, 'Bu işlem için Admin yetkisi gereklidir.', false); return; }
            const tickerSymbol = newTickerSymbolInput.value.trim().toUpperCase();
            if (!tickerSymbol) { displayMessage(trackMessage, 'Lütfen bir sembol girin.', false); return; }
            displayMessage(trackMessage, `'${escapeHtml(tickerSymbol)}' takibe alınıyor...`, true);
            try {
                const response = await fetch(`${apiUrl}/stocks/track`, {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${jwtToken}`, 'Content-Type': 'application/json' },
                    body: JSON.stringify({ tickerSymbol })
                });
                const responseData = await response.json();
                if (response.ok) {
                    displayMessage(trackMessage, responseData.message || `'${escapeHtml(tickerSymbol)}' başarıyla işlendi.`, true);
                    newTickerSymbolInput.value = '';
                    if (getTrackedSymbolsButton) getTrackedSymbolsButton.click();
                } else {
                    displayMessage(trackMessage, responseData.message || `Hata: ${response.statusText}`, false);
                }
            } catch (error) {
                console.error('Track symbol error:', error);
                displayMessage(trackMessage, 'Sembol takibe alınırken bir ağ hatası oluştu.', false);
            }
        });
    }

    if (getTrackedSymbolsButton) {
        getTrackedSymbolsButton.addEventListener('click', async () => {
            if (!jwtToken || userRole !== 'Admin') { if(trackedSymbolsList) trackedSymbolsList.innerHTML = '<li>Bu işlem için Admin yetkisi gereklidir.</li>'; return; }
            if(trackedSymbolsList) trackedSymbolsList.innerHTML = '<li>Yükleniyor...</li>';
            displayMessage(untrackMessage, '', true);
            try {
                const response = await fetch(`${apiUrl}/stocks/tracked-symbols`, {
                    method: 'GET', headers: { 'Authorization': `Bearer ${jwtToken}` }
                });
                if (response.ok) {
                    const symbols = await response.json();
                    if(trackedSymbolsList) trackedSymbolsList.innerHTML = '';
                    if (symbols.length === 0) {
                        if(trackedSymbolsList) trackedSymbolsList.innerHTML = '<li>Takip edilen sembol bulunmuyor.</li>';
                    } else {
                        symbols.forEach(symbol => {
                            const li = document.createElement('li');
                            li.textContent = escapeHtml(symbol) + ' ';
                            const untrackBtn = document.createElement('button');
                            untrackBtn.textContent = 'Takipten Çıkar';
                            untrackBtn.classList.add('untrack-btn');
                            untrackBtn.dataset.symbol = symbol;
                            untrackBtn.addEventListener('click', handleUntrackSymbol);
                            li.appendChild(untrackBtn);
                            if(trackedSymbolsList) trackedSymbolsList.appendChild(li);
                        });
                    }
                } else {
                    const errorData = await response.json();
                    if(trackedSymbolsList) trackedSymbolsList.innerHTML = `<li>Hata: ${escapeHtml(errorData.message || response.statusText)}</li>`;
                }
            } catch (error) {
                console.error('Get tracked symbols error:', error);
                if(trackedSymbolsList) trackedSymbolsList.innerHTML = '<li>Takip edilen semboller getirilirken bir ağ hatası oluştu.</li>';
            }
        });
    }


    // === YENİ: Kullanıcı Tercihleri Formu Gönderimi (PUT) ===
    if (preferenceForm) {
        preferenceForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            if (!jwtToken) {
                displayMessage(preferenceMessage, 'Bu işlem için giriş yapmalısınız.', false);
                return;
            }
            const tickerSymbol = prefHiddenTickerInput.value;
            if (!tickerSymbol) {
                displayMessage(preferenceMessage, 'Hisse senedi sembolü bulunamadı.', false);
                return;
            }

            const preferenceData = {
                notes: prefNotesInput.value.trim() === '' ? null : prefNotesInput.value.trim(),
                priceAlertUpper: prefAlertUpperInput.value ? parseFloat(prefAlertUpperInput.value) : null,
                priceAlertLower: prefAlertLowerInput.value ? parseFloat(prefAlertLowerInput.value) : null,
            };
            // The redundant `if (preferenceData.notes === "") preferenceData.notes = null;` was removed as ternary already handles it.

            displayMessage(preferenceMessage, `'${escapeHtml(tickerSymbol)}' için tercihler kaydediliyor...`, true);

            try {
                const response = await fetch(`${apiUrl}/user/preferences/${tickerSymbol}`, {
                    method: 'PUT',
                    headers: {
                        'Authorization': `Bearer ${jwtToken}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(preferenceData)
                });
                const responseData = await response.json();

                if (response.ok) { // 200 OK veya 201 Created
                    displayMessage(preferenceMessage, response.status === 201 ? 'Tercihler başarıyla oluşturuldu.' : 'Tercihler başarıyla güncellendi.', true);
                    // Optionally hide the form after successful save
                    // stockPreferenceSection.style.display = 'none';
                } else {
                    const errorMsg = responseData.message || (responseData.errors ? JSON.stringify(responseData.errors) : `Hata: ${response.statusText}`);
                    displayMessage(preferenceMessage, errorMsg, false);
                }
            } catch (error) {
                console.error('Save preferences error:', error);
                displayMessage(preferenceMessage, 'Tercihler kaydedilirken bir ağ hatası oluştu.', false);
            }
        });
    }

    // === YENİ: Kullanıcı Tercihlerini Silme Butonu (DELETE) ===
    if (deletePreferenceButton) {
        deletePreferenceButton.addEventListener('click', async () => {
            if (!jwtToken) {
                displayMessage(preferenceMessage, 'Bu işlem için giriş yapmalısınız.', false);
                return;
            }
            const tickerSymbol = prefHiddenTickerInput.value;
            if (!tickerSymbol) {
                displayMessage(preferenceMessage, 'Hisse senedi sembolü bulunamadı.', false);
                return;
            }

            if (!confirm(`'${escapeHtml(tickerSymbol)}' için tüm tercihleri silmek istediğinizden emin misiniz?`)) {
                return;
            }
            displayMessage(preferenceMessage, `'${escapeHtml(tickerSymbol)}' için tercihler siliniyor...`, true);

            try {
                const response = await fetch(`${apiUrl}/user/preferences/${tickerSymbol}`, {
                    method: 'DELETE',
                    headers: { 'Authorization': `Bearer ${jwtToken}` }
                });

                if (response.ok) { // 204 No Content veya 200 OK
                    displayMessage(preferenceMessage, `'${escapeHtml(tickerSymbol)}' için tercihler başarıyla silindi.`, true);
                    prefNotesInput.value = '';
                    prefAlertUpperInput.value = '';
                    prefAlertLowerInput.value = '';
                    if (stockPreferenceSection) stockPreferenceSection.style.display = 'none';
                } else if (response.status === 404) {
                     displayMessage(preferenceMessage, `'${escapeHtml(tickerSymbol)}' için silinecek tercih bulunamadı.`, false);
                }
                else {
                    let errorMsg = `Hata: ${response.statusText}`;
                    try {
                        const errorData = await response.json();
                        errorMsg = errorData.message || (errorData.errors ? JSON.stringify(errorData.errors) : errorMsg);
                    } catch (e) { /* JSON parse hatası olursa ignore et, text olarak kullan */ }
                    displayMessage(preferenceMessage, errorMsg, false);
                }
            } catch (error) {
                console.error('Delete preferences error:', error);
                displayMessage(preferenceMessage, 'Tercihler silinirken bir ağ hatası oluştu.', false);
            }
        });
    }


    // --- Fonksiyon Tanımları ---
    async function handleUntrackSymbol(event) {
        const symbolToUntrack = event.target.dataset.symbol;
        if (!symbolToUntrack || !jwtToken || userRole !== 'Admin') { displayMessage(untrackMessage, 'Bu işlem için Admin yetkisi veya geçerli sembol gereklidir.', false); return; }
        if (!confirm(`'${escapeHtml(symbolToUntrack)}' sembolünü takipten çıkarmak istediğinizden emin misiniz?`)) { return; }
        displayMessage(untrackMessage, `'${escapeHtml(symbolToUntrack)}' takipten çıkarılıyor...`, true);
        try {
            const response = await fetch(`${apiUrl}/stocks/track/${symbolToUntrack}`, {
                method: 'DELETE', headers: { 'Authorization': `Bearer ${jwtToken}` }
            });
            if (response.ok) {
                const responseData = await response.json();
                displayMessage(untrackMessage, responseData.message || `'${escapeHtml(symbolToUntrack)}' başarıyla takipten çıkarıldı.`, true);
                if (getTrackedSymbolsButton) getTrackedSymbolsButton.click();
            } else {
                 const errorData = await response.json().catch(() => ({ message: response.statusText }));
                displayMessage(untrackMessage, errorData.message || `Hata: ${response.statusText}`, false);
            }
        } catch (error) {
            console.error('Untrack symbol error:', error);
            displayMessage(untrackMessage, 'Sembol takipten çıkarılırken bir ağ hatası oluştu.', false);
        }
    }

    // XML'i tablo olarak gösterme fonksiyonunu güncelle (Tercihleri Düzenle Butonu ekle)
    
   function parseAndDisplayXmlAsTable(xmlString, outputElement, isSingleStock = false) {
    try {
        const parser = new DOMParser();
        const xmlDoc = parser.parseFromString(xmlString, "application/xml");

        const parseErrorNode = xmlDoc.querySelector("parsererror");
        if (parseErrorNode) {
            console.error("XML Parse Error:", parseErrorNode.textContent);
            outputElement.innerHTML = `<p class="message error">XML parse hatası oluştu. Detaylar konsolda.</p>`;
            return;
        }

        let stockNodes = [];
        const rootElement = xmlDoc.documentElement;

        if (rootElement.nodeName === "Stock") {
            stockNodes = [rootElement];
        } else if (rootElement.nodeName === "StockDataFeed" || rootElement.nodeName === "FilteredStockData") {
            stockNodes = rootElement.getElementsByTagName("Stock");
        }

        if (stockNodes.length === 0) {
            const messageNode = rootElement.getElementsByTagName("Message")[0];
            if (messageNode) {
                outputElement.innerHTML = `<p class="message">${escapeHtml(messageNode.textContent)}</p>`;
            } else {
                outputElement.innerHTML = `<p class="message">Gösterilecek hisse senedi verisi bulunamadı veya beklenmedik XML formatı.</p>`;
            }
            return;
        }

        // --- DEĞİŞİKLİK 1: Başlıklara "Tercihler" eklendi ---
        let tableHtml = '<table class="stock-table"><thead><tr>';
        const headers = ["Sembol", "Şirket", "Fiyat", "Değişim", "% Değişim", "Hacim", "Son Güncelleme", "Açıklama", "Tercihler"];
        headers.forEach(header => tableHtml += `<th>${escapeHtml(header)}</th>`);
        tableHtml += '</tr></thead><tbody>';

        for (let i = 0; i < stockNodes.length; i++) {
            const stockNode = stockNodes[i];
            const getElementText = (elementName) => {
                const node = stockNode.getElementsByTagName(elementName)[0];
                return node ? node.textContent : '';
            };
            const descriptionNode = stockNode.getElementsByTagName("Description")[0];
            const description = descriptionNode ? (descriptionNode.firstChild && descriptionNode.firstChild.nodeType === Node.CDATA_SECTION_NODE ? descriptionNode.firstChild.nodeValue : descriptionNode.textContent) : '';
            const ticker = stockNode.getAttribute("TickerSymbol") || '';
            const company = getElementText("CompanyName");
            const price = getElementText("CurrentPrice");
            const change = getElementText("Change");
            const percentChange = getElementText("PercentChange");
            const volume = getElementText("Volume");
            const updated = getElementText("LastUpdated");
            
            // --- DEĞİŞİKLİK 2: Tablo satırına "Düzenle" butonu eklendi ---
            tableHtml += `<tr>
                            <td><a href="#" class="stock-link" data-ticker="${escapeHtml(ticker)}">${escapeHtml(ticker)}</a></td>

                            <td>${escapeHtml(ticker)}</td>
                            <td>${escapeHtml(company)}</td>
                            <td>${escapeHtml(price)}</td>
                            <td>${escapeHtml(change)}</td>
                            <td>${escapeHtml(percentChange)}</td>
                            <td>${escapeHtml(volume)}</td>
                            <td>${escapeHtml(updated ? new Date(updated).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' }) : '')}</td>
                            <td>${escapeHtml(description)}</td>
                            <td><button class="edit-prefs-btn" data-ticker="${escapeHtml(ticker)}">Düzenle</button></td>
                          </tr>`;
        }

        tableHtml += '</tbody></table>';
        outputElement.innerHTML = tableHtml;

        // --- DEĞİŞİKLİK 3: Dinamik olarak eklenen butonlara olay dinleyicisi atandı ---
        // Bu, tablo DOM'a eklendikten HEMEN SONRA yapılmalıdır.
        outputElement.querySelectorAll('.edit-prefs-btn').forEach(button => {
            button.addEventListener('click', handleEditPreferences);
        });

        outputElement.querySelectorAll('.stock-link').forEach(link => {
    link.addEventListener('click', (event) => {
        event.preventDefault();
        showStockChart(link.dataset.ticker);
    });
});
    } catch (e) {
        console.error("XML parse and display error:", e);
        outputElement.innerHTML = `<p class="message error">XML verisi tablo olarak görüntülenirken bir hata oluştu: ${escapeHtml(e.message)}</p>`;
    }
    }

    // === YENİ: Tercihleri Düzenleme Formunu Açma ve Doldurma ===
    async function handleEditPreferences(event) {
        if (!jwtToken) {
            alert('Lütfen önce giriş yapın.');
            // Optionally, try to show login message or redirect to login
            if (loginMessage) displayMessage(loginMessage, 'Tercihleri düzenlemek için giriş yapmalısınız.', false);
            if (authSection) authSection.style.display = 'block'; // Show login section
            return;
        }
        const ticker = event.target.dataset.ticker;
        if (!ticker) return;

        if (preferenceMessage) displayMessage(preferenceMessage, '', true); // Önceki mesajları temizle
        if (prefTickerSymbolDisplay) prefTickerSymbolDisplay.textContent = ticker;
        if (prefHiddenTickerInput) prefHiddenTickerInput.value = ticker;

        // Formu temizle
        if (prefNotesInput) prefNotesInput.value = '';
        if (prefAlertUpperInput) prefAlertUpperInput.value = '';
        if (prefAlertLowerInput) prefAlertLowerInput.value = '';

        try {
            const response = await fetch(`${apiUrl}/user/preferences/${ticker}`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${jwtToken}` }
            });
            if (response.ok) {
                const prefData = await response.json();
                if (prefNotesInput) prefNotesInput.value = prefData.notes || '';
                if (prefAlertUpperInput) prefAlertUpperInput.value = prefData.priceAlertUpper || ''; // API null dönerse boş string olacak
                if (prefAlertLowerInput) prefAlertLowerInput.value = prefData.priceAlertLower || ''; // API null dönerse boş string olacak
            } else if (response.status === 404) {
                // No existing preferences, form remains empty which is fine.
                // Optionally display a message that no preferences exist yet
                // displayMessage(preferenceMessage, `'${escapeHtml(ticker)}' için mevcut tercih bulunamadı. Yeni oluşturabilirsiniz.`, true);
            }
            else if (response.status === 401) {
                displayMessage(preferenceMessage, 'Yetkisiz erişim. Lütfen tekrar giriş yapın.', false);
                 jwtToken = null; userRole = null; localStorage.removeItem('jwtToken'); localStorage.removeItem('userRole'); updateUIBasedOnAuthState();
            }
            else {
                displayMessage(preferenceMessage, 'Tercihler getirilirken hata oluştu.', false);
            }
        } catch (error) {
            console.error('Get preference for edit error:', error);
            displayMessage(preferenceMessage, 'Tercihler getirilirken bir ağ hatası oluştu.', false);
        }

        if (stockPreferenceSection) stockPreferenceSection.style.display = 'block'; // Tercih bölümünü göster
        window.scrollTo(0, stockPreferenceSection.offsetTop - 20); // Scroll to the preference section
    }

    // İlk UI Güncellemesi
    updateUIBasedOnAuthState();
}       
);