# Estratégia Omni Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Omni Tendência é um port direto do especialista MetaTrader "Exp_Omni_Trend". Ela combina uma média móvel com um canal baseado em ATR para detectar a tendência dominante e alternar entre exposição comprada e vendida. A versão StockSharp mantém o comportamento original, incluindo o atraso entre detecção de sinal e execução de ordem, bem como a capacidade de desabilitar segmentos individuais de entrada ou saída.

A estratégia se inscreve na série de velas configurada e alimenta cada barra finalizada para a lógica Omni Tendência. A média móvel serve como estimativa da tendência central, enquanto os multiplicadores ATR constroem envelopes de volatilidade. Os envelopes se comportam como stops trailing: preço que fecha além do limite de envelope anterior inverte a tendência, gera um novo sinal de entrada na nova direção e fecha imediatamente qualquer exposição contrária.

Se os limites opcionais de stop-loss e take-profit estiverem habilitados, eles atuam no lado do broker em passos de preço, complementando as saídas baseadas em indicadores. O tamanho da posição é controlado através da propriedade integrada `Volume` da estratégia (padrão `1`).

## Lógica de Trading

1. Calcular a média móvel escolhida (`MaType`, `MaLength`, `AppliedPrice`) no fluxo de velas.
2. Calcular ATR (`AtrLength`) e derivar duas bandas adaptativas usando `VolatilityFactor` e `MoneyRisk`. A banda superior protege posições vendidas, a banda inferior protege posições compradas.
3. Quando o preço excede a banda protetora da barra anterior, a tendência muda:
   - Um rompimento de alta (`HighPrice` acima da banda superior anterior) inverte a tendência para "acima", fecha qualquer posição vendida se permitido, e abre uma posição comprada após `SignalBar` velas completadas.
   - Um rompimento de baixa (`LowPrice` abaixo da banda inferior anterior) inverte a tendência para "abaixo", fecha qualquer posição comprada se permitido, e abre uma posição vendida após o atraso configurado.
4. Enquanto a tendência permanece de alta, a estratégia continua a solicitar saídas vendidas; a regra simétrica se aplica para uma tendência de baixa e saídas compradas. Isso espelha o comportamento do especialista MetaTrader, onde a banda oposta força constantemente exposição plana contra a direção prevalecente.
5. O gerenciamento de risco opcional monitora cada vela finalizada. Se a barra atual atingir o preço de stop ou alvo (expresso em passos de preço), a posição é fechada imediatamente, redefinindo o preço de entrada armazenado.

Os sinais são agendados através de uma fila FIFO. Quando `SignalBar` é zero, são executados no fechamento da mesma vela. Caso contrário, são acionados na abertura da vela que completa o atraso, o que replica o estilo de execução de "barra anterior" do especialista fonte.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CandleType` | Tipo de vela (período) para cálculos. | Período de 4 horas |
| `MaLength` | Período da média móvel. | 13 |
| `MaType` | Método da média móvel: simples, exponencial, suavizada ou ponderada linearmente. | Exponencial |
| `AppliedPrice` | Campo de preço para a média móvel (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado). | Fechamento |
| `AtrLength` | Período ATR para o canal de volatilidade. | 11 |
| `VolatilityFactor` | Multiplicador aplicado ao ATR ao construir o canal bruto. | 1.3 |
| `MoneyRisk` | Fator de deslocamento que afasta o canal da média móvel, idêntico ao input MQL. | 0.15 |
| `SignalBar` | Número de velas completadas a aguardar antes de agir em um sinal. | 1 |
| `EnableBuyOpen` | Permitir abrir posições compradas. | true |
| `EnableSellOpen` | Permitir abrir posições vendidas. | true |
| `EnableBuyClose` | Permitir fechar posições compradas quando tendência de baixa é detectada. | true |
| `EnableSellClose` | Permitir fechar posições vendidas quando tendência de alta é detectada. | true |
| `StopLossPoints` | Distância de stop protetor opcional em passos de preço. `0` para desabilitar. | 1000 |
| `TakeProfitPoints` | Distância do alvo de lucro opcional em passos de preço. `0` para desabilitar. | 2000 |
| `Volume` | Propriedade da estratégia que controla o tamanho da operação. | 1 |

## Notas e Recomendações

- A implementação StockSharp alimenta os mesmos valores de indicadores que o original e reproduz suas inversões de tendência. No entanto, execuções precisas dependem da fonte de dados e da latência de execução.
- Defina `SignalBar = 1` para imitar o padrão do consultor especialista, onde as ordens são executadas na abertura da próxima vela após um sinal estar disponível. Valores maiores atrasam ainda mais a execução; definir `0` executa no fechamento atual.
- Os limites de stop-loss e take-profit são expressos em pontos (passos de preço). Certifique-se de que o ativo conectado expõe um `PriceStep` válido.
- O gráfico integrado desenha a série de velas, a média móvel selecionada e as próprias operações da estratégia para validação visual rápida.
- Desabilite alternâncias específicas de entrada ou saída para restringir a estratégia à operação unilateral ou para tratar saídas manualmente.
- A estratégia não cria ordens pendentes; emite ordens de mercado usando `BuyMarket` e `SellMarket` exatamente como o placement de ordens direto do especialista fonte.

## Arquivos

- `CS/OmniTrendStrategy.cs` — Implementação em C# da estratégia.
- `README.md`, `README_ru.md`, `README_zh.md` — Documentação em inglês, russo e chinês.

O suporte a Python foi intencionalmente omitido conforme solicitado.
