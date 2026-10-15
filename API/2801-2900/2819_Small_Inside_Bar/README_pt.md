# Estratégia de Barra Interna Pequena
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Barra Interna Pequena busca um padrão compacto de barra interna seguido de uma mudança de momentum entre duas velas consecutivas. O especialista original do MetaTrader 5 foi traduzido para a API de alto nível do StockSharp e agora opera apenas em velas completadas. A abordagem é projetada para traders que preferem entradas de estilo rompimento acionadas por fases de volatilidade comprimida.

## Definição do padrão
A estratégia avalia as duas velas completadas mais recentes:

1. **Condição de barra interna** – a última vela finalizada deve estar completamente contida dentro do range da anterior.
2. **Filtro de ratio de range** – o range da barra mãe (duas barras atrás) deve ser pelo menos um múltiplo configurável do range da barra interna. O ratio padrão é 2:1.
3. **Filtros direcionais** –
   - Um setup comprado requer uma barra interna de alta formando-se na metade inferior da barra mãe junto com uma barra mãe de baixa.
   - Um setup vendido requer uma barra interna de baixa formando-se na metade superior da barra mãe junto com uma barra mãe de alta.
4. A inversão opcional troca as interpretações comprada e vendida mantendo os mesmos requisitos geométricos.

## Gerenciamento de posição
O parâmetro `OpenMode` espelha o comportamento do EA original:

- **AnySignal** – envia uma nova ordem a mercado em cada sinal. Quando existe uma posição oposta, ela é parcialmente compensada porque o StockSharp usa contas de netting.
- **SwingWithRefill** – achata a exposição oposta antes de entrar e permite múltiplas adições na mesma direção.
- **SingleSwing** – mantém no máximo uma operação direcional; novos sinais são ignorados enquanto um posição alinhada estiver aberta.

Tanto as entradas compradas quanto as vendidas podem ser habilitadas independentemente. O trading de reversão simplesmente inverte qual setup produz ordens compradas ou vendidas.

## Parâmetros
| Nome | Padrão | Descrição |
|------|--------|-----------|
| `CandleType` | Período de 1 hora | Assinatura de velas usada para detecção de padrão. |
| `RangeRatioThreshold` | 2.0 | Ratio mínimo de range mãe para interno. |
| `EnableLong` | true | Permitir trades de alta. |
| `EnableShort` | true | Permitir trades de baixa. |
| `ReverseSignals` | false | Trocar as direções de padrão comprado e vendido. |
| `OpenMode` | SwingWithRefill | Controla como a exposição existente é tratada em um novo sinal. |

## Lógica de trading
1. Assinar a série de velas configurada e aguardar barras finalizadas.
2. Manter as duas últimas velas completadas para avaliar o padrão.
3. Quando o padrão e os filtros de ratio se alinham, determinar o sinal direcional, aplicando opcionalmente a reversão.
4. Confirmar que o trading é permitido (`IsFormedAndOnlineAndAllowTrading`) e que a direção relevante está habilitada.
5. Calcular o tamanho da ordem com base no `OpenMode` selecionado e enviar uma ordem a mercado usando o volume base da estratégia.
6. Atualizar o histórico de velas interno para que a vela mais recente faça parte do próximo ciclo de avaliação.

## Notas de implementação
- A estratégia usa `StartProtection()` para habilitar o gerenciador de risco integrado (sem valores predefinidos de stop ou take-profit). Filtros extras podem ser adicionados externamente se necessário.
- O estado do indicador não é armazenado em coleções; apenas as duas últimas velas são mantidas conforme necessário para o padrão.
- O algoritmo depende exclusivamente de velas completadas, evitando cálculos intrabarra em linha com as melhores práticas da API de alto nível.
