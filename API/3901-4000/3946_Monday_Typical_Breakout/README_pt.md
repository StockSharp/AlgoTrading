# Estratégia típica de breakout de segunda-feira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Breakout Típica de Segunda-feira** é uma versão C# do MetaTrader consultor especialista `yi1ywioff50qr6` (ID do repositório 8187). O robô original monitora velas horárias e abre uma posição longa toda segunda-feira, quando a nova sessão abre acima do preço típico da barra anterior `(high + low + close) / 3`. Esta implementação reproduz a lógica de entrada dentro da estrutura estratégica de alto nível StockSharp e adiciona parâmetros de configuração detalhados para dimensionamento de posição e controle de risco.

## Lógica de negociação

1. A estratégia assina a série de velas configurada (de hora em hora por padrão).
2. No início de cada vela finalizada verifica se:
   - A vela pertence à segunda-feira.
   - O horário de abertura da vela corresponde ao parâmetro *Open Hour* configurado (padrão 09:00).
   - Não existem posições abertas ou ordens ativas.
   - O preço de abertura da vela é maior que o preço típico da barra anterior.
3. Se todas as condições forem satisfeitas, a estratégia envia uma ordem de compra a mercado com um volume calculado pelo bloco de gestão de dinheiro. As distâncias protetoras de stop-loss e take-profit são aplicadas por meio de `StartProtection`.

A estratégia nunca abre posições curtas e realizará apenas uma negociação por vela qualificada de segunda-feira.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FixedVolume` | Tamanho do lote para entradas. Defina como `0` para ativar a tabela de escalabilidade de patrimônio. | `0.1` |
| `OpenHour` | Hora da sessão de negociação (0-23) quando os sinais de segunda-feira são avaliados. | `9` |
| `StopLossPoints` | Distância em faixas de preço para o stop protetor. `0` desativa a parada. | `50` |
| `TakeProfitPoints` | Distância em faixas de preço para a meta de lucro. `0` desativa o alvo. | `20` |
| `InitialEquity` | Limite de patrimônio que ativa a escala de lote com base no patrimônio. | `600` |
| `EquityStep` | Incremento de capital necessário para aumentar o tamanho da negociação. | `300` |
| `InitialStepVolume` | Tamanho do lote usado quando o patrimônio líquido é de pelo menos `InitialEquity`. | `0.4` |
| `VolumeStep` | Tamanho de lote adicional adicionado para cada `EquityStep` alcançado. | `0.2` |
| `CandleType` | Tipo de dados Candle que orienta a estratégia (de hora em hora por padrão). | `1 hour time-frame` |

## Gestão de capital

- Quando `FixedVolume` é maior que zero a estratégia sempre utiliza o tamanho de lote fixo.
- Quando `FixedVolume` é igual a zero, a estratégia inspeciona o patrimônio do portfólio:
  - Se o patrimônio for inferior a `InitialEquity`, será utilizado o lote mínimo do instrumento.
  - Caso contrário, o volume começa em `InitialStepVolume` e aumenta em `VolumeStep` para cada `EquityStep` de capital adicional.
  - O volume final está alinhado às restrições mínimas e de etapas do instrumento.

## Gestão de risco

`StartProtection` é ativado durante `OnStarted`. As distâncias de stop-loss e take-profit são automaticamente convertidas de pontos em compensações de preço usando o instrumento `PriceStep`. Defina qualquer distância como zero para desativar esse componente.

## Notas de uso

- O EA original foi projetado para velas horárias. Prazos mais baixos podem produzir várias velas de segunda-feira na mesma hora. A porta mantém o comportamento de entrada única por vela e ainda ignorará sinais adicionais enquanto uma posição estiver aberta.
- Certifique-se de que as informações do portfólio (`Portfolio.CurrentValue`) estejam disponíveis se o bloco de dimensionamento dinâmico estiver ativado.
- A estratégia requer dados de nível 1 para executar ordens de mercado e a assinatura de vela correspondente para o `CandleType` configurado.

## Notas de conversão

- A filtragem de números mágicos MQL foi substituída pelas verificações de posição e ordem de StockSharp (`Position` e `ActiveOrders`).
- As comparações de tempo aproveitam `DateTimeOffset` do tempo de abertura da vela com `.ToLocalTime()` para permanecerem alinhadas com o tempo do gráfico.
- As ordens de proteção são tratadas pelo ajudante `StartProtection` de alto nível, em vez da colocação manual de ordens.
