const API_URL = "http://localhost:5094/api/veiculos";

function adicionarVeiculo() {
    const placa = document.getElementById("placaInput").value.trim();
    if (!placa) {
        alert("Informe a placa.");
        return;
    }

    fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ placa })
    })
        .then(async res => {
            const msg = await res.text();
            if (res.ok) {
                alert(`Veiculo de placa ${placa} adicionado. ${msg}`);
                document.getElementById("placaInput").value = "";
            } else {

                if (msg && /cadastr/i.test(msg)) {
                    alert(`Erro: placa ${placa} ja esta cadastrada no sistema.`);
                } else {
                    alert("Erro ao adicionar veiculo: " + (msg || res.statusText));
                }
            }
        })
        .catch(err => {
            alert("Erro ao adicionar veiculo: " + (err.message || err));
        });
}

function listarVeiculos() {
    fetch(API_URL)
        .then(res => res.json())
        .then(veiculos => {
            const lista = document.getElementById("listaVeiculos");
            if (!lista) return;
            lista.innerHTML = "";

            veiculos.forEach(v => {
                const li = document.createElement("li");

                const placa = v.placa || v.Placa || "";
                const dataRaw = v.dataEntrada || v.DataEntrada || null;

                let text = placa;
                if (dataRaw) {
                    const dt = new Date(dataRaw);
                    if (!isNaN(dt)) {
                        const formatted = dt.toLocaleString('pt-BR', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                        text += ` - Entrada: ${formatted}`;
                    }
                }

                li.textContent = text;
                lista.appendChild(li);
            });
        })
        .catch(err => {
            alert("Erro ao listar veículos: " + (err.message || err));
        });
}


function formatDuration(totalMinutes) {
    if (totalMinutes === null || totalMinutes === undefined) return null;
    const mins = Math.round(Number(totalMinutes) || 0);
    if (mins < 60) {
        return `${mins} minuto${mins === 1 ? '' : 's'}`;
    }
    const hours = Math.floor(mins / 60);
    const remaining = mins % 60;
    let parts = `${hours} hora${hours > 1 ? 's' : ''}`;
    if (remaining > 0) {
        parts += ` e ${remaining} minuto${remaining > 1 ? 's' : ''}`;
    }
    return parts;
}

function removerVeiculo() {
    const placa = document.getElementById("placaRemover").value.trim();
    if (!placa) {
        alert("Informe a placa para remoção.");
        return;
    }

    const horas = Number(document.getElementById("tempoHoras").value);
    const minutos = Number(document.getElementById("tempoMinutos").value);

    if ((!Number.isFinite(horas) || horas < 0) || (!Number.isFinite(minutos) || minutos < 0)) {
        alert("Informe horas e minutos válidos (não negativos).");
        return;
    }

    if (horas === 0 && minutos === 0) {
        alert("Informe o tempo de permanência ou a hora de saída (horas e/ou minutos).");
        return;
    }

    const modo = "absoluto";
    const url = `${API_URL}/${encodeURIComponent(placa)}?horas=${encodeURIComponent(horas)}&minutos=${encodeURIComponent(minutos)}&modo=${encodeURIComponent(modo)}`;

    fetch(url, { method: "DELETE" })
        .then(async res => {
            if (res.ok) {

                let data = null;
                try {
                    data = await res.json();
                } catch {
                    data = null;
                }

                const placaResp = (data && (data.Placa || data.placa)) || placa;
                const entradaRaw = data && (data.DataEntrada || data.dataEntrada);
                const saidaRaw = data && (data.DataSaida || data.dataSaida);
                const minutosTotal = data && (data.Minutos ?? data.minutos);
                const valorRaw = data && (data.Valor ?? data.valor);

                const fmtDate = dtRaw => {
                    if (!dtRaw) return null;
                    const d = new Date(dtRaw);
                    if (isNaN(d)) return null;
                    return d.toLocaleString('pt-BR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                };

                const entradaFmt = fmtDate(entradaRaw);
                const saidaFmt = fmtDate(saidaRaw);

                let valorFmt = null;
                if (valorRaw !== undefined && valorRaw !== null) {
                    const num = Number(valorRaw);
                    if (!isNaN(num)) {
                        valorFmt = num.toFixed(2).replace('.', ',');
                    } else {
                        valorFmt = String(valorRaw);
                    }
                }

                let mensagem = `Veiculo de placa ${placaResp} removido.`;
                if (minutosTotal !== undefined && minutosTotal !== null) {
                    const dur = formatDuration(minutosTotal);
                    mensagem += ` Tempo: ${dur}.`;
                }
                if (valorFmt !== null) mensagem += ` Valor: R$ ${valorFmt}.`;
                if (entradaFmt || saidaFmt) mensagem += ` Entrada: ${entradaFmt || '-'} Saida: ${saidaFmt || '-'}.`;

                alert(mensagem);

                document.getElementById("placaRemover").value = "";
                document.getElementById("tempoHoras").value = "";
                document.getElementById("tempoMinutos").value = "";

                const lista = document.getElementById("listaVeiculos");
                if (lista && lista.style.display !== 'none') listarVeiculos();
            } else if (res.status === 404) {
                alert("Veiculo nao encontrado.");
            } else {
                const text = await res.text();
                alert("Erro ao remover veiculo: " + (text || res.statusText));
            }
        })
        .catch(err => {
            alert("Erro ao remover veiculo: " + (err.message || err));
        });
}

document.addEventListener("DOMContentLoaded", () => {
    const lista = document.getElementById("listaVeiculos");
    if (lista) lista.style.display = "none";

    const btnListar = document.getElementById("btnListar");
    if (btnListar) {
        btnListar.addEventListener("click", () => {
            if (lista) lista.style.display = "block";
            listarVeiculos();
        });
    }
});