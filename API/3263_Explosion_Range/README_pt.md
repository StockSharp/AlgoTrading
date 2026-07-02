# Estratégia de Explosion Range Expansion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Explosion Range Expansion Strategy é um sistema de rompimento convertido do consultor especialista MetaTrader 5 "Explosion". O algoritmo compara o intervalo da vela completada atual com a vela anterior e abre uma posição de mercado na direção do corpo da vela sempre que a expansão do intervalo excede uma proporção configurável. A versão StockSharp mantém as funcionalidades originais de gerenciamento de dinheiro e adiciona parâmetros convenientes para controle de horário e gerenciamento do trailing stop.

## Regras de trading
- **Expansão de intervalo:** Calcula o intervalo da vela atual (`High - Low`) e o compara com o intervalo da vela anterior. Se o intervalo atual for maior que o intervalo anterior multiplicado por `Range Ratio`, um sinal é gerado.
- **Filtro de direção:**
  - Se a vela fechar acima de sua abertura e a posição atual for plana ou vendida, uma ordem de mercado comprada é enviada.
  - Se a vela fechar abaixo de sua abertura e a posição atual for plana ou comprada, uma ordem de mercado vendida é enviada.
- **Janela de trading:** Os sinais são aceitos apenas quando o tempo de fechamento da vela cair entre `Start Hour` e `End Hour` (inclusive).
- **Limite diário:** Quando `One Trade Per Day` está habilitado, apenas a primeira entrada qualificada do dia de trading é executada.
- **Pausa entre operações:** Após uma entrada de posição, a estratégia aguarda `Pause (sec)` segundos antes de aceitar um novo sinal.
- **Exposição máxima:** O tamanho líquido da posição não pode exceder `Max Positions * Order Volume`.

## Saídas e gestão de risco
- **Proteção inicial:** Níveis opcionais de stop-loss e take-profit são definidos em passos de preço e calculados a partir do preço de entrada.
- **Trailing Stop:** Quando habilitado, o stop-loss é movido mais perto do preço após atingir um limiar mínimo de lucro (`Trailing Stop + Trailing Step`). A lógica de trailing mantém o mesmo comportamento que no EA original.
- **Fechamento manual em alvos:** Se o intervalo da vela atingir o nível de stop-loss ou take-profit intrabarra, a posição é fechada usando uma ordem de mercado.

## Parâmetros
- `Candle Type` – Tipo de dados usado para assinatura de velas.
- `Order Volume` – Tamanho de cada posição em lotes.
- `Range Ratio` – Multiplicador aplicado ao intervalo da vela anterior para acionar entradas.
- `Max Positions` – Número máximo de lotes permitidos simultaneamente.
- `Pause (sec)` – Tempo mínimo em segundos entre entradas.
- `Start Hour` / `End Hour` – Filtro de horas de trading (0–23).
- `One Trade Per Day` – Restringe a estratégia a uma entrada por dia do calendário.
- `Stop Loss` – Distância inicial de stop-loss em passos de preço.
- `Take Profit` – Distância inicial de take-profit em passos de preço.
- `Trailing Stop` – Distância de trailing stop em passos de preço.
- `Trailing Step` – Distância adicional necessária antes de atualizar o trailing.

## Notas de conversão
- A estratégia usa a API `SubscribeCandles` e `Bind` de alto nível para processamento de sinais sem indicadores.
- Trailing stop, janela de trading, pausa e limite diário reproduzem a lógica MQ5 original.
- O gerenciamento de dinheiro é expresso via um único parâmetro de volume; o dimensionamento de lote baseado em porcentagem de risco do script original não é suportado nesta versão.
