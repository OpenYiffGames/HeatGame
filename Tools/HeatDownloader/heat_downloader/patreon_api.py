import requests
from typing import List, Dict

PATREON_API: str = 'https://www.patreon.com/api/posts'

class ApiResult:
    def __init__(self, data, included, meta, links=None):
        self.data = [PatreonObjectBase(**x) for x in data]
        self.included = included
        self.links = links
        self.meta = Meta(**meta)

class AttributesBase:
    def __init__(self, title: str, published_at: str, url: str, **kwargs):
        self.title = title
        self.published_at = published_at
        self.url = url
        self.__dict__.update(kwargs)

    def map(self):
        return self.__dict__

class CampainData:
    def __init__(self, id: str, type: str, **kwargs):
        self.id = id
        self.type = type
        self.__dict__.update(kwargs)

class CampainLinks:
    def __init__(self, related: str, **kwargs):
        self.related = related
        self.__dict__.update(kwargs)

class Campaign:
    def __init__(self, data, links, **kwargs):
        self.data = CampainData(**data)
        self.links = CampainLinks(**links)
        self.__dict__.update(kwargs)

class RelationshipsBase:
    def __init__(self, campaign, **kwargs):
        self.campaign = Campaign(**campaign)
        self.__dict__.update(kwargs)

class PatreonObjectBase:
    def __init__(self, attributes, id: str, type: str, relationships, **kwargs):
        self.attributes = AttributesBase(**attributes)
        self.id = id
        self.type = type
        self.relationships = relationships
        self.__dict__.update(kwargs)

    def map(self):
        return self.__dict__

class PatreonPostAttributes(AttributesBase):
    def __init__(self, title: str, published_at: str, url: str, is_paid: bool, like_count: int, teaser_text: str, **kwargs):
        super().__init__(title, published_at, url)
        self.is_paid = is_paid
        self.like_count = like_count
        self.teaser_text = teaser_text
        self.__dict__.update(kwargs)

class PatreonPost(PatreonObjectBase):
    def __init__(self, attributes, id: str, type: str, relationships, **kwargs):
        super().__init__((attributes.map()), id, type, relationships, **kwargs)
        self.attributes = PatreonPostAttributes(**(attributes.map()))

class Meta:
    def __init__(self, pagination, **kwargs):
        self.pagination = Pagination(**pagination)
        self.__dict__.update(kwargs)

class Pagination:
    def __init__(self, cursors, total, **kwargs):
        self.total = total
        self.cursors = PaginationCursor(**cursors)
        self.__dict__.update(kwargs)

class PaginationCursor:
    def __init__(self, next: str, **kwargs):
        self.next = next
        self.__dict__.update(kwargs)

class ApiFilter:
    def __init__(self, campaign_id: int, include: List[str], fields: Dict[str, List[str]], sort: str, page: PaginationCursor = None):
        self.campaign_id = campaign_id
        self.include = include
        self.fields = fields
        self.sort = sort
        self.page = page

def fetch_patreon_posts(filter: ApiFilter):
    params = {
        'filter[campaign_id]': filter.campaign_id,
        'include': ','.join(filter.include),
        'fields[post]': ','.join(filter.fields['post']),
        'fields[user]': ','.join(filter.fields['user']),
        'sort': filter.sort,
    }
    if filter.page:
        params['page[cursor]'] = filter.page.next
    json = requests.get(PATREON_API, params=params).json()
    objs = ApiResult(**json)
    return objs

def filter_patreon_posts(posts: ApiResult):
    data = filter(lambda x: x.type == 'post', posts.data)
    return [PatreonPost(**(x.map())) for x in data]