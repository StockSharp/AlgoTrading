# Estratégia UmnickTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema adaptativo de reversão à média convertido do consultor especialista MQL5 original UmnickTrader. A estratégia trabalha com uma única posição por vez, alternando entre viés comprado e vendido dependendo do resultado da operação anterior. Ela avalia o movimento de preço usando a média dos preços de abertura, máxima, mínima e fechamento, e só toma ação quando essa média se deslocou pelo menos a distância `StopBase` configurada.

## Lógica principal

- Para cada vela terminada, o preço médio `(O + H + L + C) / 4` é calculado.
- Os sinais são processados apenas quando a diferença absoluta entre a média atual e a média processada anteriormente é maior ou igual a `StopBase`. Isso imita o comportamento original do EA de esperar por um movimento suficientemente grande.
- Quando não há posição aberta, a estratégia calcula distâncias adaptativas de take-profit e stop-loss usando dois buffers circulares que armazenam as oito excursões de ganho e perda mais recentes.
- Após uma operação lucrativa, a excursão favorável máxima observada enquanto a posição estava aberta é salva no buffer de lucro (menos um preenchimento de spread), enquanto o buffer de perda recebe `StopBase + 7 * Spread`.
- Após uma operação perdedora, o buffer de lucro é redefinido para `StopBase - 3 * Spread`, o buffer de perda é atualizado com o drawdown registrado mais um preenchimento de spread, e a direção de trading é invertida para que a próxima configuração opere o lado oposto.

## Gestão de operações

- A distância padrão tanto para o take-profit quanto para o stop-loss é `StopBase`. Se o buffer acumulado de lucro ou perda exceder `StopBase / 2`, suas respectivas médias substituem a distância padrão para ampliar ou ajustar adaptativamente os níveis de saída.
- Ordens de mercado são usadas para entradas. Os preços esperados de take-profit e stop-loss são armazenados e gerenciados pela própria estratégia, portanto as posições são fechadas quando as máximas ou mínimas das velas tocam os níveis correspondentes.
- Enquanto uma posição está ativa, o movimento favorável mais alto e o drawdown mais profundo são rastreados usando extremos intrabar. Essas estatísticas alimentam os buffers quando a operação fecha.
- Apenas uma posição pode existir a qualquer momento. Um novo sinal é ignorado se a operação anterior não foi concluída.

## Parâmetros

- `StopBase` – distância base (em unidades de preço) necessária para tratar um movimento como significativo e a distância TP/SL padrão. Padrão: `0.017`.
- `TradeVolume` – volume para ordens de mercado. Padrão: `0.1`.
- `Spread` – compensação de spread aplicada ao atualizar os buffers adaptativos. Padrão: `0.0005`.
- `CandleType` – assinatura de velas usada para avaliar médias. Padrão: `TimeSpan.FromMinutes(5).TimeFrame()`.

## Classificação e filtros

- **Direção**: Ambos (mas nunca simultaneamente).
- **Estilo**: Swing adaptativo / contratendência.
- **Indicadores**: Média de preço, buffers de excursão personalizados.
- **Stops**: Stop-loss e take-profit dinâmico gerenciado pela estratégia.
- **Complexidade**: Intermediário – combina buffers com estado com dimensionamento adaptativo de saída.
- **Período**: Configurável via `CandleType`.
- **Sazonalidade / Filtros de notícias**: Não utilizados.
- **Gestão de risco**: O tamanho da posição é fixo pelo `TradeVolume`; as distâncias de saída se adaptam com base no desempenho recente.
