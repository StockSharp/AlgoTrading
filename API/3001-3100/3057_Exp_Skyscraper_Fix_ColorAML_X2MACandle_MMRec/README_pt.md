# Estratégia Exp Skyscraper Fix + ColorAML + X2MA Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão em C# do especialista MetaTrader **Exp_Skyscraper_Fix_ColorAML_X2MACandle_MMRec**.
- Combina três filtros independentes baseados em cor (canal Skyscraper Fix, nível adaptativo ColorAML, velas de dupla suavização X2MACandle).
- Cada filtro pode abrir ou fechar trades por conta própria enquanto compartilha o mesmo símbolo, permitindo seguimento de tendência cooperativo e reversões rápidas.
- Inclui um módulo de gestão monetária simplificado: quando os últimos trades de uma direção perdem repetidamente, o módulo muda para o volume reduzido (`SmallMM`).

## Lógica da estratégia
### Bloco Skyscraper Fix
1. Constrói o canal trailing Skyscraper Fix analisando o intervalo ATR e a fonte de preço escolhida (high/low ou close).
2. Quando a cor do canal se torna altista, o bloco:
   - fecha qualquer posição vendida pendente (se `Skyscraper Close Shorts` estiver habilitado);
   - pode abrir uma nova posição comprada após o atraso de sinal configurado (se `Skyscraper Buy` estiver habilitado).
3. Quando a cor se torna baixista, a lógica espelha os passos para trades vendidos.
4. Os envelopes de high/low, o multiplicador ATR (`Kv`) e o offset percentual reproduzem o comportamento do indicador original.

### Bloco ColorAML
1. Calcula o Nível de Mercado Adaptativo (AML) medindo o intervalo de duas janelas fractais consecutivas e suavizando o preço composto.
2. O indicador gera três cores: `2` (altista), `0` (baixista) e `1` (neutro). Velas neutras não desencadeiam nenhuma ação.
3. Uma cor altista fecha vendidos (se permitido) e pode abrir um comprado quando a cor mudou de altista na vela anterior inspecionada.
4. Uma cor baixista realiza as ações simétricas para trades vendidos.

### Bloco X2MACandle
1. Cascateia dois médias móveis configuráveis sobre cada componente OHLC (abertura, máxima, mínima, fechamento) para construir uma vela sintética.
2. A cor é determinada pelo corpo da vela suavizada: altista quando fechamento > abertura, baixista quando fechamento < abertura, neutro caso contrário.
3. Um pequeno limiar de lacuna (em passos de preço) aplaina corpos de velas muito pequenos para evitar flips rápidos de cor.
4. Cores altistas fecham vendidos e podem abrir comprados; cores baixistas fazem o oposto.

### Gestão monetária
1. Cada bloco mantém um histórico independente de seus próprios trades para as direções longa e curta.
2. Após o fechamento de um trade, o módulo registra se terminou com perda.
3. Se os últimos `Loss Trigger` trades para uma direção foram todos perdas, a próxima ordem desse bloco muda para o volume reduzido (`SmallMM`).
4. Quando um trade lucrativo ou neutro quebra a sequência de perdas, o módulo retorna automaticamente ao volume normal (`MM`).

## Parâmetros
| Seção | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Skyscraper | `Skyscraper Candle` | Período para amostrar velas para o indicador Skyscraper Fix. | 4h |
| Skyscraper | `Skyscraper Length` | Janela de média ATR (número de velas). | 10 |
| Skyscraper | `Skyscraper Kv` | Multiplicador de sensibilidade aplicado ao passo ATR. | 0.9 |
| Skyscraper | `Skyscraper Percentage` | Percentual adicional adicionado/removido da linha média. | 0 |
| Skyscraper | `Skyscraper Mode` | Fonte de preço (High/Low ou Close) usada para envelopes. | High/Low |
| Skyscraper | `Skyscraper Signal Bar` | Número de velas já fechadas a aguardar antes de agir sobre uma cor. | 1 |
| Skyscraper | `Skyscraper Buy` / `Skyscraper Sell` | Permitir abertura de trades longos / curtos. | true |
| Skyscraper | `Skyscraper Close Long` / `Skyscraper Close Short` | Permitir que este bloco saia de trades longos / curtos. | true |
| Skyscraper | `Skyscraper Normal Volume` | Volume base da ordem (`MM` no EA). | 0.1 |
| Skyscraper | `Skyscraper Reduced Volume` | Volume reduzido da ordem usado após uma sequência de perdas (`SmallMM`). | 0.01 |
| Skyscraper | `Skyscraper Buy Loss Trigger` / `Skyscraper Sell Loss Trigger` | Número de perdas consecutivas que mudam para o volume reduzido. | 2 |
| ColorAML | `ColorAML Candle` | Tipo de vela usado pelo indicador ColorAML. | 4h |
| ColorAML | `ColorAML Fractal` | Janela fractal (em barras) usada para o cálculo de intervalo. | 6 |
| ColorAML | `ColorAML Lag` | Parâmetro de lag que controla a suavização adaptativa. | 7 |
| ColorAML | `ColorAML Signal Bar` | Offset de vela aplicado antes de avaliar cores. | 1 |
| ColorAML | `ColorAML Buy` / `ColorAML Sell` | Habilitar entradas longas / curtas geradas pelo ColorAML. | true |
| ColorAML | `ColorAML Close Long` / `ColorAML Close Short` | Permitir ao ColorAML fechar posições longas / curtas. | true |
| ColorAML | `ColorAML Normal Volume` / `ColorAML Reduced Volume` | Volumes base e reduzido para este bloco. | 0.1 / 0.01 |
| ColorAML | `ColorAML Buy Loss Trigger` / `ColorAML Sell Loss Trigger` | Perdas consecutivas que ativam o volume reduzido. | 2 |
| X2MA | `X2MA Candle` | Período usado para a reconstrução de velas X2MACandle. | 4h |
| X2MA | `First Method` / `Second Method` | Métodos de suavização para a primeira e segunda média móvil. | SMA / JJMA |
| X2MA | `First Length` / `Second Length` | Períodos das duas etapas de suavização. | 12 / 5 |
| X2MA | `First Phase` / `Second Phase` | Fases de compatibilidade usadas pelas médias móveis Jurik. | 15 |
| X2MA | `Gap Points` | Limiar de lacuna (em passos de preço) que aplaina corpos de velas pequenos. | 10 |
| X2MA | `X2MA Signal Bar` | Offset de vela aplicado antes de reagir às cores. | 1 |
| X2MA | `X2MA Buy` / `X2MA Sell` | Permitir abertura de trades longos / curtos do bloco X2MACandle. | true |
| X2MA | `X2MA Close Long` / `X2MA Close Short` | Permitir ao bloco sair de posições longas / curtas. | true |
| X2MA | `X2MA Normal Volume` / `X2MA Reduced Volume` | Volumes base e reduzido para trades X2MACandle. | 0.1 / 0.01 |
| X2MA | `X2MA Buy Loss Trigger` / `X2MA Sell Loss Trigger` | Número de perdas consecutivas antes de mudar para o volume reduzido. | 2 |

## Dicas de uso
1. Ajuste os tipos de velas para corresponder à volatilidade do mercado (por exemplo, 1h para trading intradiário, 4h para swing trading).
2. Os três módulos podem ser ajustados independentemente — desabilitar um bloco ainda deixa os outros ativos.
3. Os limiares de gestão monetária são intencionalmente conservadores. Aumente os gatilhos se o instrumento apresentar tendências fortes e você quiser manter o volume base por mais tempo.
4. Como a estratégia depende de velas concluídas, sempre alimente-a com dados de velas que correspondam aos períodos configurados.
