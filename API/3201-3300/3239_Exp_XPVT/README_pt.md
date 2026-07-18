# Estratégia de Exp XPVT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Exp XPVT** é uma conversão do consultor especialista MetaTrader 5 *Exp_XPVT*. O sistema negocia cruzamentos entre o indicador Price and Volume Trend (PVT) e uma média móvel configurável aplicada à série PVT. Quando a linha PVT bruta cai abaixo de sua variante suavizada, a estratégia abre posições compradas, enquanto cruzamentos para cima acionam entradas vendidas. Distâncias opcionais de stop-loss e take-profit emulam o comportamento do consultor especialista original.

## Lógica do indicador
- O Price and Volume Trend acumula variações percentuais de preço ponderadas por volume usando o preço aplicado selecionado (fechamento, abertura, mediano, etc.).
- Um filtro de suavização (SMA, EMA, MA suavizado, LWMA, Jurik, T3, aproximação VIDYA ou Kaufman AMA) produz a linha de sinal.
- Um deslocamento histórico (`Signal Bar`) recria a lógica MT5: a estratégia compara os valores suavizados e brutos de uma e duas barras atrás para detectar cruzamentos e condições de saída.
- Volume de tick ou real pode ser usado para ponderação. Se o tipo de volume solicitado não estiver disponível, a estratégia recorre automaticamente à outra fonte.

## Regras de trading
1. Em cada vela concluída, calcular o valor PVT do preço aplicado e tipo de volume configurados.
2. Atualizar o indicador de suavização e armazenar os valores mais recentes de acordo com `Signal Bar`.
3. Se a barra anterior mostrou PVT acima da linha de sinal, fechar qualquer posição vendida. Se, além disso, o PVT armazenado mais recente está abaixo ou igual à linha de sinal, abrir uma posição comprada (se entradas compradas estiverem habilitadas).
4. Se a barra anterior mostrou PVT abaixo da linha de sinal, fechar qualquer posição comprada. Se, além disso, o PVT armazenado mais recente está acima ou igual à linha de sinal, abrir uma posição vendida (se entradas vendidas estiverem habilitadas).
5. Após entrar em uma operação, ordens opcionais de stop-loss e take-profit são anexadas usando as distâncias configuradas (expressas em passos de preço).
6. O gerenciamento de dinheiro imita o consultor especialista original: novas ordens usam o `Order Volume` base configurado e incluem a exposição oposta para reverter completamente ao trocar de direção.

## Parâmetros
- **Order Volume** – volume base para novas ordens e reversões.
- **Stop Loss** – distância em passos de preço para o stop protetor (0 desabilita).
- **Take Profit** – distância em passos de preço para o alvo de lucro (0 desabilita).
- **Allow Buy Entry / Allow Sell Entry** – habilitar abertura de posições compradas ou vendidas.
- **Allow Buy Exit / Allow Sell Exit** – habilitar fechamento automático de posições existentes quando o setup oposto aparecer.
- **Candle Type** – período usado para cálculos do indicador.
- **Volume Source** – escolher volume de tick ou real para ponderação PVT.
- **Smoothing Method / Length / Phase** – média móvel aplicada à linha PVT. O parâmetro de fase é usado apenas por métodos estilo Jurik.
- **Applied Price** – fórmula de preço que alimenta o PVT (fechamento, abertura, seguidor de tendência, DeMark, etc.).
- **Signal Bar** – deslocamento histórico (em barras) usado para avaliar o cruzamento, reproduzindo a implementação MT5.

## Notas
- A estratégia processa apenas velas concluídas para garantir estabilidade do indicador.
- A suavização estilo Jurik usa reflexão para encaminhar o parâmetro de fase quando o indicador o expõe.
- Quando nem volume de tick nem real está disponível, a estratégia recorre a volume zero, prevenindo acumulações espúrias.
- A chamada opcional `StartProtection` ativa o monitoramento de posição integrado do StockSharp, correspondendo à invocação única no consultor especialista original.
