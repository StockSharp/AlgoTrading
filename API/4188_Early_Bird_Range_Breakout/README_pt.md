# Breakout antecipado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Early Bird Range Breakout é uma versão C# do MetaTrader 4 consultor especialista `earlyBird1`. O sistema rastreia a máxima e a mínima de uma faixa pré-mercado configurável, aplica um filtro RSI de 14 períodos para decidir a tendência comercial e entra no primeiro rompimento assim que a sessão regular for aberta. Ele preserva a restrição de negociação única por direção do consultor especialista original, a lógica de rastreamento controlada pela volatilidade e a disciplina de fechamento diário.

## Lógica estratégica
### Construção de intervalo
* **Janela de tempo** – o intervalo é calculado entre `Range Start Hour` e `Range End Hour` (após aplicar a lógica de deslocamento do horário de verão). Cada vela que cruza esta janela expande o limite superior/inferior.
* **Buffer de entrada** – um deslocamento configurável em pips é adicionado acima do intervalo alto e subtraído abaixo do intervalo baixo para imitar o buffer de breakout `±2/Fakt` do script `±2/Fakt`.
* **Redefinição diária** – o intervalo, os gatilhos de entrada e os contadores de negociação são redefinidos com a primeira vela concluída de cada novo dia de negociação.

### Filtro direcional
* **RSI em aberturas** – a estratégia alimenta o RSI com preços de abertura de velas, correspondendo à implementação MT4 que usou `iRSI(..., PRICE_OPEN)`.
* **Seleção de polarização** – quando RSI está acima de 50 apenas o gatilho longo é armado; quando RSI é 50 ou inferior, apenas o gatilho curto está ativo. Isso garante uma configuração direcional única por vela, assim como o EA original.

### Regras de entrada
* **Sessão de negociação** – novas posições são permitidas apenas em dias úteis entre `Session Start` e `Session End` após a formação do intervalo de breakout.
* **Tentativa única por lado** – assim que uma posição longa (ou curta) é aberta, o lado correspondente é desativado pelo resto do dia, refletindo os contadores de negociação diários no código MT4.
* **Mudança de hedge** – com `Allow Hedging` ativado, a estratégia pode reverter de curta para longa (ou vice-versa) enviando volume suficiente para nivelar a exposição existente e mudar imediatamente de direção. Quando a cobertura está desativada, as entradas são ignoradas, a menos que a posição seja plana.

### Regras de saída
* **Risco e meta fixos** – os níveis de stop-loss e take-profit são expressos em pips. A meta de lucro é automaticamente limitada pela distância de parada e pela largura do intervalo, reproduzindo a lógica `MathMin` do consultor especialista original.
* **Trailing orientado pela volatilidade** – quando o intervalo da vela atual excede o intervalo médio de 16 períodos multiplicado por `Trailing Risk`, e a negociação tem lucro de pelo menos `Trailing Trigger`, o stop é seguido pela distância do stop completo enquanto o take-profit é puxado para mais perto (metade do gatilho final), correspondendo ao comportamento de `OrderModify` no código MQL.
* **Encerramento da sessão** – na hora de fechamento configurada, as negociações lucrativas são fechadas imediatamente. As posições perdedoras movem seu lucro para o preço de entrada, assim como a aplicação do ponto de equilíbrio do MT4.

## Parâmetros
* **Negociação Automática** – interruptor mestre de habilitação para entradas automatizadas.
* **Permitir Hedging** – permite a reversão na direção oposta mesmo quando uma posição já está aberta.
* **Direção de negociação** – limita a estratégia apenas para compra (`1`), apenas para venda (`2`) ou ambas as direções (`0`).
* **Volume** – volume de pedidos para entradas no mercado.
* **Take Profit (pips)** – distância máxima para a meta de lucro; a distância efetiva é limitada pelo stop loss e pela largura do intervalo.
* **Stop Loss (pips)** – distância de parada de proteção fixa em pips.
* **Trailing Trigger (pips)** – excursão favorável mínima necessária antes que a lógica de trailing possa ajustar o stop e o take-profit.
* **Risco de trilha** – multiplicador aplicado ao intervalo médio de velas de 16 períodos ao avaliar se a volatilidade é alta o suficiente para trilhar.
* **Buffer de entrada (pips)** – deslocamento de pip aplicado aos limites do intervalo ao calcular os níveis de rompimento.
* **Hora/minuto de início da sessão** – início da janela de negociação ativa (tempo do gráfico antes do ajuste do horário de verão).
* **Hora de Fim da Sessão** – fim da janela de negociação para novas posições.
* **Hora de fechamento** – hora após a qual as posições são forçadas a atingir o ponto de equilíbrio ou fechar.
* **Hora de início do intervalo/Hora de término do intervalo** – horas que definem o intervalo de pré-sessão usado para intervalos.
* **Início do horário de verão/Início do horário de inverno** – marcadores do dia do ano usados para alternar entre deslocamentos de uma e duas horas, imitando a lógica `Sommerzeit/Winterzeit`.
* **RSI Comprimento** – número de períodos para o filtro RSI (padrão 14).
* **Tipo de vela** – período principal que orienta os cálculos (o padrão é velas de 15 minutos).

## Notas adicionais
* O tamanho do pip é derivado do nível de preço atual (≥ 10 unidades → `0.01`, caso contrário `0.0001`) exatamente como o cálculo `Fakt` no script MT4.
* As estatísticas finais usam as últimas 16 velas concluídas, excluindo a barra atual, correspondendo à lógica de média original.
* A estratégia StockSharp usa posições líquidas, portanto, posições longas e curtas simultâneas são emuladas pela compra ou venda excessiva da exposição existente quando o hedge está ativado.
* Somente a implementação C# é fornecida; nenhuma versão Python acompanha esta estratégia.
