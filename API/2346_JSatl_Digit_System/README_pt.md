# JSatl Sistema Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O JSatl Sistema Digit usa uma Média Móvel Jurik (JMA) para determinar a direção da tendência.
A estratégia mede a inclinação da JMA e abre uma posição quando o preço confirma a direção da inclinação.

Uma posição comprada é aberta quando a JMA está subindo e o preço de fechamento está acima da média.
Uma posição vendida é aberta quando a JMA está caindo e o preço de fechamento está abaixo da média.
Sinais opostos fecham qualquer posição aberta.

## Detalhes

- **Critérios de entrada**: Inclinação da JMA com confirmação de preço.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `JmaLength` = 14
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: JMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing (4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
