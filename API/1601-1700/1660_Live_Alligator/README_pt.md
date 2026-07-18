# Estratégia Alligator ao Vivo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões de tendência usando uma configuração dinâmica do Alligator e vários filtros EMA.
Abre uma nova posição quando as linhas do Alligator mudam de direção e cinco EMAs confirmam o movimento.
Um filtro opcional de horário de trading limita as entradas a uma sessão escolhida.
A posição aberta é fechada quando o preço cruza uma média móvel suavizada trailing.

- **Critérios de entrada**
  - Alligator lips acima de jaws com teeth abaixo de jaws e a barra anterior com lips abaixo de jaws -> abrir comprado após uma tendência de baixa.
  - Alligator lips abaixo de jaws com teeth acima de jaws e a barra anterior com lips acima de jaws -> abrir vendido após uma tendência de alta.
  - Cinco EMAs sobre preços de fechamento, ponderado, típico, mediano e de abertura devem estar estritamente ordenadas na direção da tendência.
- **Critérios de saída**
  - O preço cruza a SMMA trailing baseada em `TrailPeriod`.
  - Stop-loss opcional aplicado na abertura da operação.
- **Indicadores utilizados**
  - Médias Móveis Suavizadas para as linhas do Alligator e o stop trailing.
  - Médias Móveis Exponenciais em diferentes tipos de preço.

Os parâmetros permitem configurar o período base do Alligator, período de confirmação EMA, período trailing, stop-loss e janela de horário de trading.
