# Estratégia de Fraktrak XonaX Advanced
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma conversão em C# do consultor especialista do MetaTrader 5 **Fraktrak xonax.mq5**. O robô original rastreia fractais de Williams e abre operações quando o preço rompe o nível fractal mais recente. A versão StockSharp mantém a mesma ideia aproveitando recursos da API de alto nível como assinaturas de velas, helpers integrados de gestão monetária e proteção automática de operações.

## Lógica de trading

1. **Detecção de fractais** – o algoritmo mantém uma janela de cinco velas. Quando a vela do meio cria uma máxima mais alta (ou mínima mais baixa) do que seus vizinhos, o preço é salvo como o último fractal superior (ou inferior).
2. **Sinais de rompimento** – quando uma vela concluída toca ou ultrapassa o nível fractal atual, a estratégia se prepara para operar:
   - Rompimento de fractal superior → abrir posição comprada (ou posição vendida quando o *Modo Reversão* está habilitado).
   - Rompimento de fractal inferior → abrir posição vendida (ou posição comprada quando o *Modo Reversão* está habilitado).
3. **Gestão de posições** – a estratégia convertida reproduz o comportamento do MetaTrader:
   - Fechamento opcional da posição oposta antes de abrir uma nova.
   - Stop-loss e take-profit iniciais são definidos de acordo com as distâncias em pips configuradas.
   - Um trailing stop de dois estágios move o nível protetor após o preço avançar pelo *Passo de Trailing* especificado.
4. **Gestão monetária** – escolher entre lote fixo ou percentual de risco baseado em patrimônio. Quando o modo de risco está ativo, o algoritmo estima o volume usando o patrimônio do portfólio, o tamanho do passo de preço e a distância de stop configurada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `StopLossPips` | Distância de stop-loss expressa em pips. Definir como zero para desabilitar o nível de stop-loss. |
| `TakeProfitPips` | Distância de take-profit em pips. Zero desabilita o alvo. |
| `TrailingStopPips` | Distância base do trailing stop. Requer que `TrailingStepPips` seja maior que zero. |
| `TrailingStepPips` | Distância adicional que o preço deve percorrer antes do trailing stop avançar. |
| `ReverseMode` | Inverter as regras de rompimento (vender fractais superiores, comprar fractais inferiores). |
| `CloseOpposite` | Quando verdadeiro, qualquer posição oposta é fechada antes de abrir uma nova operação. |
| `ManagementMode` | Selecionar entre gestão monetária `FixedLot` ou `RiskPercent`. |
| `ManagementValue` | Valor usado pelo modo de gestão monetária ativo (tamanho de lote ou percentual). |
| `CandleType` | Série de velas usada para detecção de fractais e decisões de trading. |

## Notas de uso

- O tamanho do pip é derivado automaticamente do passo de preço do instrumento. Ativos com três ou cinco dígitos decimais são tratados como instrumentos de pip fracionário (0.1 pip). Ajustar os parâmetros pip adequadamente.
- A lógica do trailing stop corresponde ao expert original: requer que tanto a distância de trailing quanto o passo adicional sejam positivos. Caso contrário, o trailing é ignorado.
- A gestão monetária no modo de risco assume que o custo do passo de preço está disponível. Se não estiver, a estratégia recorre a um cálculo simplificado baseado na distância de preço bruta.
- Habilitar *Fechar Oposto* para emular o comportamento do consultor especialista onde um novo rompimento fecha a operação em andamento antes de entrar na direção oposta.

## Arquivos

- `CS/FraktrakXonaxAdvancedStrategy.cs` – implementação da estratégia.
- `README.md` – documento atual.
- `README_ru.md` – descrição em russo.
- `README_zh.md` – descrição em chinês.
