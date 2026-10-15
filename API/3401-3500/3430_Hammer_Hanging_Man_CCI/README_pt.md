# Martelo e Homem Enforcado com CCI Confirmação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reimplementa o especialista MetaTrader "AH HM CCI" em StockSharp. Observa o martelo e o castiçal do homem enforcado
padrões e requer confirmação do Commodity Channel Index (CCI) antes de entrar em uma negociação. Os filtros extras de confirmação
elimina padrões fracos e ajuda a alinhar as entradas com a mudança de impulso sinalizada por CCI.

A lógica é executada apenas em velas concluídas e usa uma média móvel simples e curta (SMA) para definir a tendência predominante. O anterior
a vela deve ser um martelo em tendência de baixa com sobrevenda CCI para comprar, ou um homem enforcado em tendência de alta com sobrecompra CCI para vender. Saídas
são gerenciados quando CCI cruza níveis de gatilho configuráveis, replicando a lógica de saída baseada em voto do especialista original.

## Lógica de negociação

1. **Filtro de tendência** – O ponto médio da vela anterior deve estar abaixo (para posições compradas) ou acima (para posições vendidas) de um SMA calculado em
preços de fechamento. Isso imita a verificação de tendência da média móvel do assistente original.
2. **Detecção de padrões** – A estratégia avalia a barra anterior e verifica:
   - Corpo inteiramente no terço superior da faixa de velas.
   - Espaço entre a abertura/fechamento da vela anterior e a vela anterior.
   - Contexto direcional (martelo para tendência de baixa, homem enforcado para tendência de alta).
3. **CCI confirmação** – O CCI da barra anterior deve estar abaixo do limite longo ou acima do limite curto. Os valores padrão
corresponda ao modelo MetaTrader (40 para posições longas e 60 para posições curtas).
4. **Saídas de posição** – As posições existentes são fechadas quando CCI ultrapassa os limites de saída inferior ou superior. Cruzando de
abaixo fecha posições compradas; cruzar por cima fecha o short.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Tipo de vela e intervalo de tempo usado para reconhecimento de padrões. | `TimeSpan.FromMinutes(15)` |
| `CciPeriod` | Número de barras utilizadas pelo Commodity Channel Index. | `11` |
| `MaPeriod` | Número de barras no filtro de tendência SMA. | `5` |
| `LongConfirmationThreshold` | Valor máximo CCI permitido para um sinal de martelo. | `40` |
| `ShortConfirmationThreshold` | Valor mínimo CCI permitido para um sinal de homem suspenso. | `60` |
| `ExitUpperThreshold` | CCI nível que aciona saídas após uma travessia ascendente. | `70` |
| `ExitLowerThreshold` | Nível de saída secundário para sinais antecipados. | `30` |

Todos os parâmetros estão disponíveis para otimização. Os limites aceitam valores negativos, para que você possa adaptar a estratégia a outras
mercados ou níveis de ruído apertando ou afrouxando os filtros.

## Gerenciamento de ordens

- **Entradas** usam ordens de mercado dimensionadas como `Volume + |Position|`, garantindo que as reversões sejam executadas em uma única negociação.
- **Saídas** dependem exclusivamente dos cruzamentos CCI para ficar perto do especialista MetaTrader. Adicione chamadas `StartProtection` se precisar
níveis explícitos de stop-loss ou take-profit.

## Dicas de uso

- Aplique a estratégia em instrumentos líquidos onde as lacunas e caudas das velas são informativas.
- Experimente valores mais longos de `CciPeriod` e `MaPeriod` para suavizar o ruído ao negociar prazos mais longos.
- Diminuir `LongConfirmationThreshold` ou aumentar `ShortConfirmationThreshold` reduzirá o número de negociações, mas melhorará
seletividade.
