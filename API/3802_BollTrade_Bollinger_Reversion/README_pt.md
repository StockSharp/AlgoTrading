# Estratégia de reversão BollTrade Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Reversão BollTrade Bollinger** é uma estratégia StockSharp de alto nível convertida do clássico consultor especialista BollTrade MetaTrader. Ele negocia um único instrumento usando bandas Bollinger e espera por variações de preço além das bandas, além de um buffer de pip adicional. Quando uma vela fecha acima da banda superior a estratégia abre uma posição curta, e quando uma vela fecha abaixo da banda inferior ela abre uma posição longa. Todas as decisões são tomadas em velas finalizadas para evitar reagir a dados incompletos.

## Lógica de negociação

1. Assine o tipo de vela configurado e calcule Bollinger Bandas com período e desvio selecionados.
2. Calcule uma compensação de preço adicional expressa em unidades pip para imitar o buffer original que forçou as negociações a entrarem mais profundamente no território de sobrecompra/sobrevenda.
3. Quando o preço de fechamento de uma vela concluída estiver abaixo da banda inferior menos o deslocamento, abra uma posição longa. Quando estiver acima da banda superior mais o deslocamento, abra uma posição curta.
4. Para cada negociação aberta, a estratégia armazena níveis de stop-loss e take-profit definidos em unidades pip. Essas saídas emulam o consultor especialista original que fechava posições quando o lucro ou perda flutuante cruzava distâncias de pip predefinidas.
5. As posições são fechadas quando o intervalo da vela ultrapassa o limite de stop-loss ou de take-profit. Nenhum dimensionamento ou pirâmide adicional é executado.

## Gestão de capital

* O parâmetro `Lots` define o tamanho da posição base.
* Quando `LotIncrease` está ativado, o volume é dimensionado proporcionalmente ao valor atual do portfólio em relação ao valor observado no início da estratégia, até um limite de segurança de 500 lotes. Isso reproduz a lógica de dimensionamento vinculada ao saldo da versão MetaTrader.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Take Profit (pips)** | Distância em pips usada para calcular o nível de lucro a partir do preço de entrada. Defina como zero para desativar a saída de lucro. |
| **Stop Loss (pips)** | Distância em pips usada para calcular o nível de stop loss a partir do preço de entrada. Defina como zero para desativar a saída de stop loss. |
| **Deslocamento de banda** | Distância adicional de pip adicionada além da banda Bollinger antes de abrir uma negociação. |
| **Bollinger Período** | Número de velas usadas para a média móvel das bandas Bollinger. |
| **Bollinger Desvio** | Multiplicador de desvio padrão para a largura das bandas Bollinger. |
| **Volume Básico** | Volume base de negociação em lotes. |
| **Volume de escala** | Quando ativado, aumenta o volume de pedidos com base no crescimento do valor do portfólio. |
| **Tipo de vela** | Tipo de vela (período de tempo) usado para geração de sinal. |

## Notas

* A estratégia funciona apenas com velas finalizadas e, portanto, precisa de dados históricos para aquecimento antes da negociação ao vivo.
* Os níveis de stop-loss e take-profit são avaliados em intervalos de velas, o que se aproxima da lógica original baseada em ticks, permanecendo compatível com o nível superior API.
* Os recursos de proteção da estrutura StockSharp (`StartProtection`) são ativados para proteger contra exposição acidental à posição quando a estratégia para inesperadamente.
