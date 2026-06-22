# Estratégia ColorJFatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa a direção da inclinação de uma Média Móvel Jurik (JMA) para gerar operações. A JMA aproxima o indicador "ColorJFatl_Digit" do expert MQL5 original. Uma posição comprada é aberta quando a JMA vira ascendente, enquanto uma posição vendida é aberta quando a JMA vira descendente. Posições opostas são fechadas quando a inclinação se reverte.

O sistema negocia em ambas as direções e não emprega stops rígidos por padrão. É adequado para instrumentos onde as mudanças de tendência podem ser capturadas por uma média móvel adaptativa suavizada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A inclinação da JMA muda de negativa para positiva.
  - **Vendido**: A inclinação da JMA muda de positiva para negativa.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: A inclinação da JMA torna-se negativa.
  - **Vendido**: A inclinação da JMA torna-se positiva.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `JMA Length` = 5
  - `Timeframe` = 4 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
