const API_BASE = "http://localhost:8080";

const weatherForm = document.getElementById('weatherForm');
const statsButton = document.getElementById('statsButton');
const loadingSpinner = document.getElementById('loadingSpinner');
const errorMessage = document.getElementById('errorMessage');
const weatherResults = document.getElementById('weatherResults');
const statsResults = document.getElementById('statsResults');

weatherForm.addEventListener('submit', handleWeatherSearch);
statsButton.addEventListener('click', handleStatsRequest);

async function handleWeatherSearch(e) {
    e.preventDefault();
    
    const city = document.getElementById('cityInput').value.trim();
    const days = document.getElementById('daysInput').value || 7;
    
    if (!city) {
        showError('Molimo unesite naziv grada');
        return;
    }
    
    hideAll();
    showLoading();
    
    try {
        const response = await fetch(`${API_BASE}/api/weather?city=${encodeURIComponent(city)}&days=${days}`);
        
        if (!response.ok) {
            throw new Error('Greška pri preuzimanju podataka');
        }
        
        const data = await response.json();
        displayWeatherResults(data);
    } catch (error) {
        showError(error.message || 'Došlo je do greške pri preuzimanju podataka');
    } finally {
        hideLoading();
    }
}

async function handleStatsRequest() {
    hideAll();
    showLoading();
    
    try {
        const response = await fetch(`${API_BASE}/api/stats`);
        
        if (!response.ok) {
            throw new Error('Greška pri preuzimanju statistike');
        }
        
        const data = await response.json();
        displayStatsResults(data);
    } catch (error) {
        showError(error.message || 'Došlo je do greške pri preuzimanju statistike');
    } finally {
        hideLoading();
    }
}

function displayWeatherResults(data) {
    const html = `
        <div class="weather-header">
            <h3>🌤️ ${data.city}</h3>
            <p>Koordinate: ${data.latitude.toFixed(2)}°N, ${data.longitude.toFixed(2)}°E</p>
        </div>
        
        <div class="stats-summary">
            <div class="stat-card">
                <h4>Prosečna temperatura</h4>
                <div class="value">${data.avgTemp.toFixed(1)}°C</div>
            </div>
            <div class="stat-card">
                <h4>Min temperatura</h4>
                <div class="value">${data.minTemp.toFixed(1)}°C</div>
            </div>
            <div class="stat-card">
                <h4>Max temperatura</h4>
                <div class="value">${data.maxTemp.toFixed(1)}°C</div>
            </div>
            <div class="stat-card">
                <h4>Prosečan UV indeks</h4>
                <div class="value">${data.avgUvIndex.toFixed(1)}</div>
            </div>
        </div>
        
        <h3>Detaljna prognoza</h3>
        <table>
            <thead>
                <tr>
                    <th>Datum</th>
                    <th>Max Temp</th>
                    <th>Min Temp</th>
                    <th>Prosek</th>
                    <th>UV Indeks</th>
                </tr>
            </thead>
            <tbody>
                ${generateWeatherTableRows(data.daily)}
            </tbody>
        </table>
        
        <button class="back-button" onclick="goBack()">← Nazad</button>
    `;
    
    weatherResults.innerHTML = html;
    weatherResults.classList.remove('hidden');
}

function generateWeatherTableRows(daily) {
    return daily.time.map((date, index) => {
        const avgTemp = (daily.tempMax[index] + daily.tempMin[index]) / 2;
        return `
            <tr>
                <td>${date}</td>
                <td>${daily.tempMax[index].toFixed(1)}°C</td>
                <td>${daily.tempMin[index].toFixed(1)}°C</td>
                <td>${avgTemp.toFixed(1)}°C</td>
                <td>${daily.uvIndexMax[index].toFixed(1)}</td>
            </tr>
        `;
    }).join('');
}

function displayStatsResults(data) {
    const html = `
        <h2>📊 Statistika servera</h2>
        <div class="stats-content">${escapeHtml(data.summary)}</div>
        <button class="back-button" onclick="goBack()">← Nazad</button>
    `;
    
    statsResults.innerHTML = html;
    statsResults.classList.remove('hidden');
}

function showLoading() {
    loadingSpinner.classList.remove('hidden');
}

function hideLoading() {
    loadingSpinner.classList.add('hidden');
}

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');
    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}

function hideAll() {
    weatherResults.classList.add('hidden');
    statsResults.classList.add('hidden');
    errorMessage.classList.add('hidden');
}

function goBack() {
    hideAll();
    document.getElementById('cityInput').value = '';
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}