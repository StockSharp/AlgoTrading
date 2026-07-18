# Estratégia de Tendência KWAN RDP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp do especialista MetaTrader `Exp_KWAN_RDP`. A lógica calcula o oscilador KWAN RDP combinando três indicadores padrão e suavizando seu produto:

1. **DeMarker** — mede a relação entre máximas e mínimas recentes para avaliar o esgotamento do momentum.
2. **Money Flow Index** — avalia preço e volume para detectar condições de sobrecompra ou sobrevenda.
3. **Momentum** — captura a velocidade das mudanças de preço usando o período selecionado.
4. O valor bruto `100 * DeMarker * MFI / Momentum` é suavizado com uma média móvel configurável (SMA, EMA, SMMA, WMA ou Jurik).

A inclinação do oscilador suavizado produz sinais de trading:

- **Virada de alta (inclinação ascendente)**: fechar posições vendidas e opcionalmente abrir uma posição comprada.
- **Virada de baixa (inclinação descendente)**: fechar posições compradas e opcionalmente abrir uma posição vendida.
- Barras neutras (inclinação plana) não acionam ações.

## Parâmetros

- `CandleType` — série de velas para cálculos de indicadores (padrão: período H1).
- `DeMarkerPeriod` — período do indicador DeMarker.
- `MfiPeriod` — período do Money Flow Index.
- `MomentumPeriod` — período do indicador Momentum.
- `SmoothingLength` — comprimento da média móvel de suavização.
- `Smoothing` — método de suavização (Simple, Exponential, Smoothed, Weighted, Jurik).
- `EnableLongEntries` / `EnableShortEntries` — permite abrir posições compradas ou vendidas.
- `CloseLongsOnReverse` / `CloseShortsOnReverse` — fechar posições opostas quando um sinal de reversão aparecer.
- `TakeProfitPercent` / `StopLossPercent` — proteção opcional baseada em porcentagem aplicada através de `StartProtection`.

## Regras de trading

1. Subscrever a série de velas configurada e calcular DeMarker, MFI, Momentum e o valor KWAN suavizado em cada vela terminada.
2. Detectar a direção da inclinação do último valor do oscilador em relação ao anterior.
3. Quando a inclinação sobe, fechar vendidos (se habilitado) e abrir um comprado se o trading comprado estiver permitido e nenhuma posição comprada estiver ativa.
4. Quando a inclinação desce, fechar comprados (se habilitado) e abrir um vendido se o trading vendido estiver permitido e nenhuma posição vendida estiver ativa.
5. Usar as porcentagens opcionais de stop-loss e take-profit para proteger as posições com a proteção da plataforma.

## Notas

- Os sinais são processados apenas em velas completadas para evitar ruído intrabarra.
- O cálculo do DeMarker usa suavização interna para corresponder à implementação do MetaTrader.
- Todos os comentários no código C# são escritos em inglês conforme as diretrizes do projeto.
