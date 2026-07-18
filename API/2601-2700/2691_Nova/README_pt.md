# Estratégia Nova
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Conversão do assessor especialista "Nova" do MetaTrader 5 que monitora o momentum do preço em um número fixo de segundos.
- Funciona com qualquer tipo de candle escolhido pelo parâmetro `CandleType` e avalia a lógica apenas em candles concluídos.
- Rastreia os melhores preços de compra e venda usando dados Level1 e armazena seus valores de `SecondsAgo` segundos antes.
- Entra em uma posição **comprada** quando o candle anterior é altista e o ask atual é maior que o ask armazenado em pelo menos `StepPips`.
- Entra em uma posição **vendida** quando o candle anterior é baixista e o bid atual é menor que o ask armazenado em pelo menos `StepPips`.
- Aplica níveis automáticos de stop-loss e take-profit usando proteção do StockSharp se os parâmetros correspondentes forem maiores que zero.
- Após uma perda (ativação do stop-loss), o volume do próximo trade é multiplicado por `LossCoefficient`; após uma saída lucrativa o volume é redefinido para `BaseVolume`.

## Parâmetros
- `SecondsAgo` – número de segundos entre o snapshot de preço de referência e o momento de avaliação atual.
- `StepPips` – filtro de rompimento em pips; convertido para unidades de preço usando o passo de preço do título (instrumentos de 3/5 decimais são ajustados por ×10).
- `BaseVolume` – tamanho inicial do trade; normalizado para o passo de volume da bolsa e limites mín/máx.
- `StopLossPips` – distância em pips para o stop-loss de proteção (0 o desativa).
- `TakeProfitPips` – distância em pips para o take-profit de proteção (0 o desativa).
- `LossCoefficient` – multiplicador aplicado ao último volume executado após um trade perdedor.
- `CandleType` – fonte de candles usada para sinais (período, tick, range, etc.).

## Notas Adicionais
- A estratégia requer dados Level1 (melhor bid/ask) para replicar o comportamento original do MT5; os candles fornecem um fallback usando o preço de fechamento quando o Level1 não está disponível.
- O recálculo de volume respeita `Security.VolumeStep`, `Security.MinVolume` e `Security.MaxVolume` para evitar ordens inválidas.
- As conversões de preço dependem de `Security.PriceStep` e `Security.Decimals` para que a estratégia se adapte tanto a símbolos forex de 4/5 dígitos quanto a outros instrumentos.
- Nenhuma versão Python é fornecida para esta estratégia.
