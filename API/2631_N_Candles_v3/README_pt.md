# Estratégia N Candles v3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia verifica as últimas velas concluídas e procura uma sequência onde as últimas *N* barras compartilham a mesma direção (todas altistas ou todas baixistas). Quando tal sequência aparece, entra na direção da sequência respeitando um limite sobre quantas posições podem ser abertas simultaneamente. A implementação migra o assessor especializado original do MetaTrader 5 para a API de alto nível do StockSharp.

## Lógica de trading
- O motor se subscreve ao tipo de vela configurado e processa apenas barras concluídas.
- Para cada vela concluída, a direção do corpo é avaliada: altista, baixista ou neutra (doji).
- Velas doji reiniciam o contador interno. Caso contrário, o contador aumenta quando a vela atual tem a mesma direção que as anteriores. Assim que o contador atinge o parâmetro `Identical Candles`, a estratégia emite uma nova ordem.
- Sinais comprados fecham primeiro qualquer exposição vendida existente e depois adicionam uma unidade comprada enquanto o volume comprado total permanecer abaixo de `Max Positions * Volume`.
- Sinais vendidos funcionam simetricamente para sequências baixistas.

## Gestão de risco
- Após cada operação executada, a estratégia coloca novas ordens de stop-loss e take-profit de proteção baseadas no preço médio de entrada da posição ativa.
- As distâncias são medidas em passos de preço do instrumento: `Take Profit Points` multiplica o passo para calcular o alvo acima (comprado) ou abaixo (vendido) da entrada; `Stop Loss Points` usa a mesma ideia para o stop de proteção.
- Um trailing stop escalonado pode substituir o stop inicial uma vez que o preço se mova `Trailing Stop Points` em favor da posição. O stop só é movido quando o preço avançou pelo menos `Trailing Step Points` além do nível de trailing anterior.

## Parâmetros
- **Candle Type** – Período ou fonte de velas a analisar.
- **Identical Candles** – Número necessário de velas consecutivas com a mesma direção para acionar uma entrada.
- **Volume** – Tamanho da ordem para cada nova entrada em unidades do instrumento.
- **Max Positions** – Número máximo de unidades de entrada que podem estar abertas na mesma direção simultaneamente.
- **Take Profit Points** – Distância do take-profit em múltiplos do passo de preço do instrumento.
- **Stop Loss Points** – Distância do stop-loss em múltiplos do passo de preço do instrumento.
- **Trailing Stop Points** – Distância do preço atual usada para ativar e manter o trailing stop. Definir como zero para desabilitar o trailing.
- **Trailing Step Points** – Distância extra em passos de preço que deve ser coberta antes de mover novamente o trailing stop.

## Notas adicionais
- A estratégia opera de forma neteada: quando aparece um sinal na direção oposta, qualquer exposição existente do outro lado é fechada antes de adicionar uma nova posição.
- Todas as ordens de proteção são recriadas após cada preenchimento para manter seu volume sincronizado com o tamanho da posição aberta.
- Garantir que o instrumento forneça um `PriceStep` diferente de zero; caso contrário, o valor de passo padrão de 1 é usado.
