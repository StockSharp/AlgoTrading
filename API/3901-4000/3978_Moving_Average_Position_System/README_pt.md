# Estratégia do sistema de posição média móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O Moving Average Position System é uma porta direta do consultor especialista MetaTrader 4 "MovingAveragePositionSystem.mq4". A estratégia monitora uma média móvel retrospectiva longa e reage aos cruzamentos de preços que ocorrem em velas concluídas. Ele oferece suporte à seleção manual de lotes e a uma rotina opcional de escalonamento de volume do tipo martingale que reage aos lucros e perdas acumulados expressos em MetaTrader pontos.

## Lógica de negociação

1. **Detecção de sinal**
   - O sistema calcula uma média móvel configurável (simples, exponencial, suavizada ou linear ponderada).
   - Quando o fechamento da vela finalizada mais recentemente cruza a média móvel na direção oposta do fechamento anterior, a estratégia abre uma nova posição.
   - É permitida apenas uma posição por sentido; se a estratégia já for longa, ela não aumentará a posição até que a atual seja fechada, e o mesmo se aplica às negociações curtas.
2. **Gerenciamento de posição**
   - Se a vela que acabou de fechar ficar abaixo da média móvel enquanto uma posição longa estiver aberta, a posição será imediatamente fechada no mercado.
   - Se a vela fechar acima da média móvel enquanto uma posição curta estiver aberta, a venda será fechada.
   - Um take-profit estilo MetaTrader expresso em etapas de preço (pontos) pode ser ativado por meio dos parâmetros de estratégia. Caso contrário, as paradas são gerenciadas pelo cruzamento da média móvel.
3. **Gerenciamento de dinheiro**
   - Quando o bloco martingale está habilitado, a estratégia acumula PnL realizado e flutuante em MetaTrader pontos para o ciclo atual.
   - Se as perdas acumuladas excederem o limite de perda configurado, o próximo volume de negociação será duplicado (embora nunca exceda o tamanho máximo do lote) e todas as posições abertas serão achatadas.
   - Quando os lucros acumulados excedem a meta de lucro configurada, o volume é redefinido para o tamanho do lote inicial e quaisquer posições abertas são fechadas para garantir ganhos.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **MaType** | Método de cálculo de média móvel: Simples, Exponencial, Suavizado ou LinearWeighted. Espelha a entrada `TypeMA` do especialista original. |
| **MaPeríodo** | Período de lookback para a média móvel (padrão 240). |
| **MaShift** | Deslocamento para frente aplicado aos valores da média móvel antes de gerar sinais. Equivalente à entrada `SdvigMA`. |
| **Tipo de vela** | Tipo de dados Candle usado para cálculos de sinal. O padrão é velas de período de 1 hora. |
| **Volume inicial** | O volume usado antes da rotina martingale o modificar. Corresponde à entrada `Lots`. |
| **Volume inicial** | Tamanho do lote base para o qual o martingale é redefinido após um ciclo lucrativo (`StarLots`). |
| **Volume máximo** | Limite superior para o volume de negociação (`MaxLots`). A estratégia reduz para metade o volume de trabalho se este limite for excedido. |
| **LossThresholdPips** | Limite de perda em MetaTrader pontos que aciona um evento de duplicação de volume (`LossPips`). |
| **Limite de LucroPips** | Meta de lucro em pontos que redefine o volume de volta ao valor inicial (`ProfitPips`). |
| **TakeProfitPips** | Distância fixa opcional de lucro em pontos aplicada por meio do auxiliar de proteção integrado (`TakeProfit`). |
| **UsarGerenciamento de Dinheiro** | Ativa ou desativa a rotina de dimensionamento de posição tipo martingale (`MM`). |

## Notas de uso

- Configure a estratégia com o mesmo símbolo e intervalo de tempo que foram usados em MetaTrader; o período padrão de 240 funciona bem com velas H1, replicando a configuração original.
- Os limites de pontos pressupõem que o instrumento fornece um `PriceStep` e um `StepPrice` válidos. Para símbolos que não possuem esses metadados, pode ser necessário ajustar os limites manualmente.
- Como o código original recalcula as margens antes de cada entrada, a porta executa uma etapa simplificada de normalização de volume que reduz pela metade o tamanho da negociação sempre que excede `MaxVolume`. Controles de risco adicionais podem ser adicionados por meio dos provedores de risco StockSharp padrão, se necessário.
- Somente velas concluídas acionam entradas e saídas, espelhando a implementação MQL que verificou os valores `Close[1]` e `Close[2]` em cada nova barra.

## Arquivos

- `CS/MovingAveragePositionSystemStrategy.cs` – implementação em C# da lógica de negociação usando a estratégia de alto nível StockSharp API.
- `README.md` – Documentação em inglês (este arquivo).
- `README_ru.md` – Documentação russa.
- `README_zh.md` – documentação chinesa.
