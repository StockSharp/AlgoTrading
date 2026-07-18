# Estratégia Last ZZ50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Last ZZ50 reproduz o consultor especializado "Last ZZ50" de Vladimir Karputov para MetaTrader.
Usa o indicador ZigZag para rastrear os três pontos de inflexão mais recentes e coloca ordens pendentes no ponto médio das duas últimas pernas do ZigZag.
A abordagem tenta se juntar aos rompimentos do último swing enquanto cancela ou reposiciona ordens sempre que a estrutura do ZigZag muda.

## Lógica de negociação
- **Detecção de pivôs** – Um indicador ZigZag (profundidade 12, desvio 5, backstep 3 por padrão) fornece os pivôs mais recentes rotulados A (mais recente), B e C.
- **Ordem do segmento BC** – Quando o pivô C difere de B e o novo pivô A não invalida a direção do segmento, a estratégia coloca uma ordem pendente em `(B + C) / 2`.
  - Se o segmento BC está subindo a ordem é comprada, caso contrário é vendida.
  - O tipo limite versus stop é selecionado de acordo com o preço atual relativo ao ponto médio.
- **Ordem do segmento AB** – A mesma lógica de ponto médio é aplicada ao segmento AB, novamente usando ordens limite ou stop dependendo do preço atual.
- **Filtro de sessão** – A negociação é limitada a um dia da semana configurável e janela intradiária (padrão segunda-feira 09:01 a sexta-feira 21:01). Fora da janela, a estratégia cancela ordens pendentes e pode opcionalmente nivelar qualquer posição.
- **Saída com trailing** – Uma vez que uma posição ganha mais do que a soma dos limiares de trailing stop e trailing step, uma ordem stop protetora é arrastada atrás do preço para fixar os lucros.

## Gestão de risco
- O volume de ordens pendentes é igual ao parâmetro multiplicador vezes o volume mínimo negociável do instrumento.
- Tanto as ordens AB quanto BC são canceladas e recriadas sempre que os pivôs do ZigZag mudam, impedindo que ordens obsoletas permaneçam no livro.
- Os trailing stops só são ativados depois que a posição está confortavelmente no lucro, reduzindo saídas prematuras em condições agitadas.

## Parâmetros
- `LotMultiplier` – Multiplicador aplicado ao volume mínimo negociável ao enviar ordens.
- `ZigZagDepth`, `ZigZagDeviation`, `ZigZagBackstep` – Valores de configuração para o indicador ZigZag.
- `TrailingStopPips`, `TrailingStepPips` – Distância e limite de ativação para o trailing stop medido em pips.
- `StartDay`, `EndDay`, `StartTime`, `EndTime` – Limites da sessão de negociação.
- `CloseOutsideSession` – Se as posições devem ser niveladas quando o filtro de tempo está inativo.
- `CandleType` – Série de candles usada para cálculos do ZigZag (padrão 1 hora).

## Indicadores
- **ZigZag** – Fornece pontos pivô que impulsionam a colocação de ordens e a validação de estrutura.
